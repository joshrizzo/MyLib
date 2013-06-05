using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

public static class MyHtmlHelpers
{
   private static object DefaultHtmlAttributes = new { @type = "text" };

   public static MvcHtmlString ScaffoldDropDown<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, object HtmlAttributes)
   {
      return ScaffoldEdit(html, expression, html.DropDownListFor(expression, selectList ?? new SelectList(new string[] { " " }), HtmlAttributes).ToHtmlString());
   }

   public static MvcHtmlString ScaffoldDropDown<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
   {
      return ScaffoldEdit(html, expression, html.DropDownListFor(expression, selectList ?? new SelectList(new string[] { " " })).ToHtmlString());
   }

   public static MvcHtmlString ScaffoldInput<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, object HtmlAttributes)
   {
      return ScaffoldEdit(html, expression, html.TextBoxFor(expression, HtmlAttributes).ToHtmlString());
   }

   public static MvcHtmlString ScaffoldEdit<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, string input = "AutoGenerate", string labelText = "AutoGenerate", bool includeValidation = true)
   {
      var validation = html.ValidationMessageFor(expression, "*");
      var sb = new StringBuilder();
      sb.Append("<div class=\"editor-label\">");
      sb.Append(labelText == "AutoGenerate" ?
         html.LabelFor(expression).ToHtmlString() :
         labelText);
      sb.Append("</div>");
      sb.Append("<div class=\"editor-field\">");
      sb.Append(input == "AutoGenerate" ?
         html.EditorFor(expression).ToHtmlString() :
         input);
      sb.Append(validation.IsNotNull() && includeValidation ? validation.ToHtmlString() : string.Empty);
      sb.Append("</div>");
      return MvcHtmlString.Create(sb.ToString());
   }

   public static MvcHtmlString ScaffoldDisplay<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, string labelText = "AutoGenerate")
   {
      return html.ScaffoldDisplay<TModel>
      (
         label: labelText == "AutoGenerate" ? html.LabelFor(expression).ToHtmlString() : labelText,
         value: html.DisplayFor(expression).ToHtmlString()
      );
   }

   public static MvcHtmlString ScaffoldDisplay<TModel>(this HtmlHelper<TModel> html, string label, string value)
   {
      var sb = new StringBuilder();
      sb.AppendLine("<div class=\"display-label\">");
      sb.AppendLine(label);
      sb.AppendLine("</div>");
      sb.AppendLine("<div class=\"display-field\">");
      sb.AppendLine(value);
      sb.AppendLine("</div>");
      return MvcHtmlString.Create(sb.ToString());
   }

   public static MvcHtmlString Link(string link, string text = "")
   {
      return new MvcHtmlString("<a href=\"{0}\">{1}</a>".FormatWith(link, string.IsNullOrWhiteSpace(text) ? link : text));
   }

   public static MvcHtmlString Link(this HtmlHelper html, string link, string text = "")
   {
      return Link(link, text);
   }

   public static MvcHtmlString MailTo(this HtmlHelper html, string emailAddress, string text = "", string emailSubject = "", string emailBody = "")
   {
      var qs = HttpUtility.ParseQueryString(string.Empty);
      if (emailSubject.IsNotEmptyOrWhiteSpace()) qs["subject"] = emailSubject;
      if (emailBody.IsNotEmptyOrWhiteSpace()) qs["body"] = emailBody;

      return new MvcHtmlString("<a href=\"mailto:{0}?{1}\">{2}</a>".FormatWith(emailAddress, qs.ToString(), text.IsEmptyOrWhiteSpace() ? emailAddress : text));
   }

   public static MvcHtmlString ToJQueryAutocompleteSource(this HtmlHelper html, string[] options)
   {
      return new MvcHtmlString("[\"{0}\"]".FormatWith(options.Join("\", \"")));
   }

   public static MvcHtmlString CheckBoxFor<TModel>(this HtmlHelper<TModel> html, Expression<Func<TModel, string>> expression)
   {
      // get the name of the property
      string propertyName = html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(ExpressionHelper.GetExpressionText(expression));

      // get the value of the property
      Func<TModel, string> compiled = expression.Compile();
      string booleanStr = compiled(html.ViewData.Model);

      // convert it to a Boolean
      bool isChecked = false;
      Boolean.TryParse(booleanStr, out isChecked);

      TagBuilder checkbox = new TagBuilder("input");
      checkbox.MergeAttribute("id", Regex.Replace(propertyName, @"\W", "_"));
      checkbox.MergeAttribute("name", propertyName);
      checkbox.MergeAttribute("type", "checkbox");
      checkbox.MergeAttribute("value", "true");
      if (isChecked)
         checkbox.MergeAttribute("checked", "checked");

      TagBuilder hidden = new TagBuilder("input");
      hidden.MergeAttribute("name", propertyName);
      hidden.MergeAttribute("type", "hidden");
      hidden.MergeAttribute("value", "false");

      return MvcHtmlString.Create(checkbox.ToString(TagRenderMode.SelfClosing) + hidden.ToString(TagRenderMode.SelfClosing));
   }
}
