using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Elmah;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeAttribute : System.Web.Mvc.AuthorizeAttribute
{
	protected override void HandleUnauthorizedRequest(System.Web.Mvc.AuthorizationContext filterContext)
	{
		var user = filterContext.HttpContext.User;
		if (user.Identity.IsAuthenticated)
			filterContext.Result = new HttpStatusCodeResult(403);
		else
			base.HandleUnauthorizedRequest(filterContext);
	}
}

public class HandleErrorAttribute : System.Web.Mvc.HandleErrorAttribute
{
	public override void OnException(ExceptionContext context)
	{
		base.OnException(context);

		var e = context.Exception;
		if (!context.ExceptionHandled   // if unhandled, will be logged anyhow
			|| RaiseErrorSignal(e)      // prefer signaling, if possible
			|| IsFiltered(context))     // filtered?
			return;

		LogException(e);
	}

	private static bool RaiseErrorSignal(Exception e)
	{
		var context = HttpContext.Current;
		if (context == null)
			return false;
		var signal = ErrorSignal.FromContext(context);
		if (signal == null)
			return false;
		signal.Raise(e, context);
		return true;
	}

	private static bool IsFiltered(ExceptionContext context)
	{
		var config = context.HttpContext.GetSection("elmah/errorFilter")
					 as ErrorFilterConfiguration;

		if (config == null)
			return false;

		var testContext = new ErrorFilterModule.AssertionHelperContext(
								  context.Exception, HttpContext.Current);

		return config.Assertion.Test(testContext);
	}

	private static void LogException(Exception e)
	{
		var context = HttpContext.Current;
		ErrorLog.GetDefault(context).Log(new Error(e, context));
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class MembershipPasswordAttribute : ValidationAttribute
{
	public override bool IsValid(object value)
	{
		if (this.ErrorMessage.IsEmptyOrWhiteSpace())
			this.ErrorMessage = "Invalid Password";

		if (value != null && value.ToString().IsNotEmptyOrWhiteSpace() && (
			(Membership.MinRequiredPasswordLength.IsNotNull() &&
				value.ToString().Length < Membership.MinRequiredPasswordLength) ||
			(Membership.MinRequiredNonAlphanumericCharacters.IsNotNull() &&
				value.ToString().GetMatches(@"\w").Count < Membership.MinRequiredNonAlphanumericCharacters) ||
			(Membership.PasswordStrengthRegularExpression.IsNotEmptyOrWhiteSpace() &&
				!value.ToString().IsMatchingTo(Membership.PasswordStrengthRegularExpression))))
			return false;
		else
			return true;
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireHttpsAttribute : System.Web.Mvc.RequireHttpsAttribute
{
	public override void OnAuthorization(AuthorizationContext filterContext)
	{
		if (filterContext.HttpContext.Request.IsLocal && !filterContext.HttpContext.Request.IsSecureConnection)
		{
			var secureUri = new UriBuilder(filterContext.HttpContext.Request.Url);
			secureUri.Scheme = "https";
			secureUri.Port = int.Parse(ConfigurationManager.AppSettings["LocalHTTPSPort"]);
			HttpContext.Current.Response.Redirect(secureUri.ToString());
		}
		else
			base.OnAuthorization(filterContext);
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class IsEqualTo : ValidationAttribute
{
	private object RequiredValue { get; set; }

	public IsEqualTo(object value)
	{
		RequiredValue = value;
	}

	public override bool IsValid(object value)
	{
		if (value.IsNull() || value.ToString() == RequiredValue.ToString())
			return true;
		else
			return false;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class IsLessThanOrEqualTo : ValidationAttribute
{
	private double RequiredValue { get; set; }

	public IsLessThanOrEqualTo(double value)
	{
		RequiredValue = value;
	}

	public override bool IsValid(object value)
	{
		if (value.IsNull() || value.CastTo<double>() <= RequiredValue)
			return true;
		else
			return false;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class IsGreaterThanOrEqualTo : ValidationAttribute
{
	private double RequiredValue { get; set; }

	public IsGreaterThanOrEqualTo(double value)
	{
		RequiredValue = value;
	}

	public override bool IsValid(object value)
	{
		if (value.IsNull() || value.CastTo<double>() >= RequiredValue)
			return true;
		else
			return false;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class IsLessThan : ValidationAttribute
{
	private double RequiredValue { get; set; }

	public IsLessThan(double value)
	{
		RequiredValue = value;
	}

	public override bool IsValid(object value)
	{
		if (value.IsNull() || value.CastTo<double>() < RequiredValue)
			return true;
		else
			return false;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class IsGreaterThan : ValidationAttribute
{
	private double RequiredValue { get; set; }

	public IsGreaterThan(double value)
	{
		RequiredValue = value;
	}

	public override bool IsValid(object value)
	{
		if (value.IsNull() || value.CastTo<double>() > RequiredValue)
			return true;
		else
			return false;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public abstract class BaseDateRangeAttribute : ValidationAttribute, IClientValidatable, IMetadataAware
{
	// TODO: How can we set these programmatically?
	private const string clientDateFormat = "m/d/yy";
	private const string dateFormat = "M/d/yyyy";

	private static class DefaultErrorMessages
	{
		public const string Range = "'{0}' must be a date between {1:d} and {2:d}.";
		public const string Min = "'{0}' must be a date after {1:d}";
		public const string Max = "'{0}' must be a date before {2:d}.";
	}

	protected DateTime minDate = DateTime.MinValue;
	protected DateTime maxDate = DateTime.MaxValue;

	public bool SuppressDataTypeUpdate { get; set; }

	public override bool IsValid(object value)
	{
		if (value == null || !(value is DateTime))
		{
			return true;
		}
		else
		{
			DateTime dateValue = (DateTime)value;
			return minDate <= dateValue && dateValue <= maxDate;
		}
	}

	public override string FormatErrorMessage(string name)
	{
		EnsureErrorMessage();
		return String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, minDate, maxDate);
	}

	private void EnsureErrorMessage()
	{
		//  normally we'd pass the default error message in the constructor
		// but here the default message depends on whether we have one or both of the range ends
		// This method is used to inject a default error message if none has been set
		if (string.IsNullOrEmpty(ErrorMessage)
			&& string.IsNullOrEmpty(ErrorMessageResourceName)
			&& ErrorMessageResourceType == null)
		{
			string message;
			if (minDate == DateTime.MinValue)
			{
				if (maxDate == DateTime.MaxValue)
				{
					throw new ArgumentException("Must set at least one of Min and Max");
				}
				message = DefaultErrorMessages.Max;
			}
			else
			{
				if (maxDate == DateTime.MaxValue)
				{
					message = DefaultErrorMessages.Min;
				}
				else
				{
					message = DefaultErrorMessages.Range;
				}
			}
			ErrorMessage = message;
		}
	}

	protected static DateTime ParseDate(string dateValue, DateTime defaultValue)
	{
		if (string.IsNullOrWhiteSpace(dateValue))
		{
			return defaultValue;
		}
		return DateTime.ParseExact(dateValue, dateFormat, CultureInfo.InvariantCulture);
	}

	protected static string FormatDate(DateTime dateTime, DateTime defaultValue)
	{
		if (dateTime == defaultValue)
		{
			return "";
		}
		return dateTime.ToString(dateFormat);
	}

	public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
	{
		return new[] { new ModelClientValidationRangeDateRule(FormatErrorMessage(metadata.GetDisplayName()), minDate, maxDate, dateFormat, clientDateFormat) };
	}

	public void OnMetadataCreated(ModelMetadata metadata)
	{
		if (!SuppressDataTypeUpdate)
		{
			metadata.DataTypeName = "Date";
		}
	}

	public class ModelClientValidationRangeDateRule : ModelClientValidationRule
	{
		public ModelClientValidationRangeDateRule(string errorMessage, DateTime minValue, DateTime maxValue, string dateFormat, string clientDateFormat)
		{
			ErrorMessage = errorMessage;
			ValidationType = "rangedate";

			ValidationParameters["min"] = minValue.ToString(dateFormat);
			ValidationParameters["max"] = maxValue.ToString(dateFormat);
			ValidationParameters["format"] = clientDateFormat;
		}
	}
}

public class DateRangeAttribute : BaseDateRangeAttribute
{
	public string Min
	{
		get { return FormatDate(minDate, DateTime.MinValue); }
		set { minDate = ParseDate(value, DateTime.MinValue); }
	}

	public string Max
	{
		get { return FormatDate(maxDate, DateTime.MaxValue); }
		set { maxDate = ParseDate(value, DateTime.MaxValue); }
	}
}

public class PastDateAttribute : BaseDateRangeAttribute
{
	public string Min
	{
		get { return FormatDate(minDate, DateTime.MinValue); }
		set { minDate = ParseDate(value, DateTime.MinValue); }
	}

	public PastDateAttribute()
	{
		maxDate = DateTime.Now.Date;
	}
}

public class FutureDateAttribute : BaseDateRangeAttribute
{
	public string Max
	{
		get { return FormatDate(maxDate, DateTime.MinValue); }
		set { maxDate = ParseDate(value, DateTime.MinValue); }
	}

	public FutureDateAttribute()
	{
		minDate = DateTime.Now.Date;
	}
}

public class LayoutInjecterAttribute : ActionFilterAttribute
{
	private readonly string _masterName;
	public LayoutInjecterAttribute(string masterName)
	{
		_masterName = masterName;
	}

	public override void OnActionExecuted(ActionExecutedContext filterContext)
	{
		base.OnActionExecuted(filterContext);
		var result = filterContext.Result as ViewResult;
		if (result != null)
		{
			result.MasterName = _masterName;
		}
	}
}