using System;
using System.Collections.Generic;
using System.Data;
//using System.Linq;
using System.Reflection;

namespace MyLib
{
   //class Sample
   //{
   //    static void Main(string[] args)
   //    {
   //        // create sequence 
   //        Item[] items = new Item[] { new Book{Id = 1, Price = 13.50, Genre = "Comedy", Author = "Jim Bob"}, 
   //                                    new Book{Id = 2, Price = 8.50, Genre = "Drama", Author = "John Fox"},  
   //                                    new Movie{Id = 1, Price = 22.99, Genre = "Comedy", Director = "Phil Funk"},
   //                                    new Movie{Id = 1, Price = 13.40, Genre = "Action", Director = "Eddie Jones"}};


   //        var query1 = from i in items
   //                     where i.Price > 9.99
   //                     orderby i.Price
   //                     select i;

   //        // load into new DataTable
   //        DataTable table1 = query1.ToDataTable();

   //        // load into existing DataTable - schemas match            
   //        DataTable table2 = new DataTable();
   //        table2.Columns.Add("Price", typeof(int));
   //        table2.Columns.Add("Genre", typeof(string));

   //        var query2 = from i in items
   //                     where i.Price > 9.99
   //                     orderby i.Price
   //                     select new { i.Price, i.Genre };

   //        query2.ToDataTable(table2, LoadOption.PreserveChanges);


   //        // load into existing DataTable - expand schema + autogenerate new Id.
   //        DataTable table3 = new DataTable();
   //        DataColumn dc = table3.Columns.Add("NewId", typeof(int));
   //        dc.AutoIncrement = true;
   //        table3.Columns.Add("ExtraColumn", typeof(string));

   //        var query3 = from i in items
   //                     where i.Price > 9.99
   //                     orderby i.Price
   //                     select new { i.Price, i.Genre };

   //        query3.ToDataTable(table3, LoadOption.PreserveChanges);

   //        // load sequence of scalars.

   //        var query4 = from i in items
   //                     where i.Price > 9.99
   //                     orderby i.Price
   //                     select i.Price;

   //        var DataTable4 = query4.ToDataTable();
   //    }

   //    public class Item
   //    {
   //        public int Id { get; set; }
   //        public double Price { get; set; }
   //        public string Genre { get; set; }
   //    }

   //    public class Book : Item
   //    {
   //        public string Author { get; set; }
   //    }

   //    public class Movie : Item
   //    {
   //        public string Director { get; set; }
   //    }

   //}

   public static class DataSetOperators
   {
      public static DataTable ToDataTable<T>(this T source, string tableName = null)
      {
         var wrappedSource = new List<T>() { source };
         return new ObjectShredder<T>().Shred(wrappedSource, null, null, tableName);
      }

      public static DataTable ToDataTable<T>(this IEnumerable<T> source, string tableName = null)
      {
         return new ObjectShredder<T>().Shred(source, null, null, tableName);
      }

      public static DataTable ToDataTable<T>(this IEnumerable<T> source,
                                       DataTable table, LoadOption? options)
      {
         return new ObjectShredder<T>().Shred(source, table, options);
      }

   }

   public class ObjectShredder<T>
   {
      private FieldInfo[] _fi;
      private PropertyInfo[] _pi;
      private Dictionary<string, int> _ordinalMap;
      private Type _type;

      public ObjectShredder()
      {
         _type = typeof(T);
         _fi = _type.GetFields();
         _pi = _type.GetProperties();
         _ordinalMap = new Dictionary<string, int>();
      }

      public DataTable Shred(IEnumerable<T> source, DataTable table, LoadOption? options, string tableName = null)
      {
         if (typeof(T).IsPrimitive)
         {
            return ShredPrimitive(source, table, options, tableName);
         }

         if (table == null)
         {
            table = new DataTable(tableName.IsNotEmptyOrWhiteSpace() ? tableName : typeof(T).Name);
         }

         // now see if need to extend datatable base on the type T + build ordinal map
         table = ExtendTable(table, typeof(T));

         table.BeginLoadData();
         using (IEnumerator<T> e = source.GetEnumerator())
         {
            while (e.MoveNext())
            {
               if (options != null)
               {
                  table.LoadDataRow(ShredObject(table, e.Current), (LoadOption)options);
               }
               else
               {
                  table.LoadDataRow(ShredObject(table, e.Current), true);
               }
            }
         }
         table.EndLoadData();
         return table;
      }

      public DataTable ShredPrimitive(IEnumerable<T> source, DataTable table, LoadOption? options, string tableName = null)
      {
         if (table == null)
         {
            table = new DataTable(tableName.IsNotEmptyOrWhiteSpace() ? tableName : typeof(T).Name);
         }

         if (!table.Columns.Contains("Value"))
         {
            table.Columns.Add("Value", typeof(T));
         }

         table.BeginLoadData();
         using (IEnumerator<T> e = source.GetEnumerator())
         {
            Object[] values = new object[table.Columns.Count];
            while (e.MoveNext())
            {
               values[table.Columns["Value"].Ordinal] = e.Current;

               if (options != null)
               {
                  table.LoadDataRow(values, (LoadOption)options);
               }
               else
               {
                  table.LoadDataRow(values, true);
               }
            }
         }
         table.EndLoadData();
         return table;
      }

      public DataTable ExtendTable(DataTable table, Type type)
      {
         // value is type derived from T, may need to extend table.
         foreach (FieldInfo f in type.GetFields())
         {
            if (!_ordinalMap.ContainsKey(f.Name))
            {
               DataColumn dc = table.Columns.Contains(f.Name) ? table.Columns[f.Name]
                  : table.Columns.Add(f.Name, f.FieldType);
               _ordinalMap.Add(f.Name, dc.Ordinal);
            }
         }

         foreach (PropertyInfo p in type.GetProperties())
         {
            if (!_ordinalMap.ContainsKey(p.Name))
            {
               Type colType = p.PropertyType;

               if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
               {
                  colType = colType.GetGenericArguments()[0];
               }

               DataColumn dc = table.Columns.Contains(p.Name) ? table.Columns[p.Name]
                  : table.Columns.Add(p.Name, colType);
               _ordinalMap.Add(p.Name, dc.Ordinal);
            }
         }

         return table;
      }

      public object[] ShredObject(DataTable table, T instance)
      {

         FieldInfo[] fi = _fi;
         PropertyInfo[] pi = _pi;

         if (instance.GetType() != typeof(T))
         {
            ExtendTable(table, instance.GetType());
            fi = instance.GetType().GetFields();
            pi = instance.GetType().GetProperties();
         }

         Object[] values = new object[table.Columns.Count];
         foreach (FieldInfo f in fi)
         {
            values[_ordinalMap[f.Name]] = f.GetValue(instance);
         }

         foreach (PropertyInfo p in pi)
         {
            values[_ordinalMap[p.Name]] = p.GetValue(instance, null);
         }
         return values;
      }
   }
}
