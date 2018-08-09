using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Dash
{
    public enum CardTypes
    {
        Success,
        Warning,
        Info,
        Error,
        Primary
    }

    public enum DashClasses
    {
        Btn,
        BtnSm,
        BtnSuccess,
        BtnWarning,
        BtnInfo,
        BtnError,
        BtnPrimary,
        BtnSecondary,
        DashAjax,
        DashConfirm,
        DashPrompt
    }

    public enum HttpVerbs
    {
        Get,
        Put,
        Post,
        Delete
    }

    public static class Html
    {
        private static List<DashClasses> Buttons = new List<DashClasses> { DashClasses.BtnError, DashClasses.BtnInfo, DashClasses.BtnPrimary,
            DashClasses.BtnSuccess, DashClasses.BtnWarning };
        private static List<DashClasses> Dialogs = new List<DashClasses> { DashClasses.DashConfirm, DashClasses.DashPrompt };

        public enum InputFieldType { Text, Email, Number, DateTime, Date, Tel, Password }

        public static IHtmlContent AuthorizedActionLink(this IHtmlHelper helper, string linkText, string action, string controller, object routeValues = null, object htmlAttributes = null,
            bool returnEmpty = true, bool hasAccess = true)
        {
            if (hasAccess)
            {
                return helper.ActionLink(linkText, action, controller, routeValues, htmlAttributes);
            }
            if (returnEmpty)
            {
                return HtmlString.Empty;
            }
            else if (htmlAttributes == null)
            {
                return new HtmlString(linkText);
            }
            else
            {
                var span = new TagBuilder("span");
                span.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
                span.InnerHtml.Append(linkText);
                return span;
            }
        }

        /*
        public static IHtmlContent AuthorizedButton(this IHtmlHelper helper, string text, string controller, string action, DashClasses? btnType = null,
            DashClasses? ajaxType = null, object htmlAttributes = null, bool hasAccess = true)
        {
            return AuthorizedButton(helper, text, controller, action, null, btnType, ajaxType, htmlAttributes, hasAccess);
        }
        */

        public static IHtmlContent AuthorizedButton(this IHtmlHelper helper, string text, string action, string controller, object routeValues = null, DashClasses? btnType = null, bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return HtmlString.Empty;
            }

            var classList = new List<string>();
            classList.Merge("btn");
            classList.Merge("mr-1");
            classList.Merge((btnType ?? DashClasses.BtnPrimary).ToCssClass());

            var urlHelper = new UrlHelper(helper.ViewContext);
            var btn = new TagBuilder("a");
            btn.AddCssClass(classList.Combine());
            btn.MergeAttribute("href", urlHelper.Action(action, controller, routeValues));
            btn.InnerHtml.Append(text);
            return btn;
        }

        public static TagBuilder AuthorizedMenu(this IHtmlHelper helper, string linkText, string action, string controller, string icon, bool authorized, object linkAttributes = null)
        {
            if (!authorized)
            {
                return null;
            }

            var a = new TagBuilder("a");
            a.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(linkAttributes));
            a.MergeAttribute("href", new UrlHelper(helper.ViewContext).Action(action, controller));
            a.MergeAttribute("title", linkText);
            var span = new TagBuilder("span");
            span.InnerHtml.Append(linkText);
            a.InnerHtml.AppendHtml(helper.Icon(icon));
            a.InnerHtml.Append(" ");
            a.InnerHtml.AppendHtml(span);
            var li = new TagBuilder("li");
            li.InnerHtml.AppendHtml(a);
            return li;
        }

        public static MvcForm BeginCustomForm(this IHtmlHelper helper, string action, string controller, object routeValues = null, string title = null, string dataLoadEvent = null, string dataUrl = null, object htmlAttributes = null, bool ajaxForm = true, HttpVerbs method = HttpVerbs.Post)
        {
            var htmlAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!htmlAttr.ContainsKey("class"))
            {
                htmlAttr["class"] = "";
            }
            htmlAttr["class"] = MergedList(htmlAttr.ContainsKey("class") ? htmlAttr["class"] : "", new string[] { "container form-horizontal p-5" }).Combine();
            if (!title.IsEmpty())
            {
                htmlAttr["data-pjax-title"] = title.Trim();
            }
            if (!htmlAttr.ContainsKey("id"))
            {
                htmlAttr["id"] = $"{action.ToLower()}{controller.UppercaseFirst()}Form";
            }
            if (!htmlAttr.ContainsKey("data-load-event") && !dataLoadEvent.IsEmpty())
            {
                htmlAttr["data-load-event"] = dataLoadEvent;
            }
            if (!htmlAttr.ContainsKey("data-url") && !dataUrl.IsEmpty())
            {
                htmlAttr["data-url"] = dataUrl;
            }
            if (!htmlAttr.ContainsKey("data-confirm"))
            {
                htmlAttr["data-confirm"] = Core.DiscardChanges;
            }

            if (method != HttpVerbs.Post && method != HttpVerbs.Get)
            {
                // @todo can we switch to just using method later?
                htmlAttr["data-method"] = method;
            }

            var mvcForm = helper.BeginForm(action, controller, new RouteValueDictionary(routeValues), FormMethod.Post, true, htmlAttr);
            helper.ViewContext.Writer.Write(helper.AntiForgeryToken());
            return mvcForm;
        }

        public static IDisposable BeginBodyContent(this IHtmlHelper helper, string title = null, string url = null, string loadEvent = null, string unloadEvent = null)
        {
            var div = new TagBuilder("div");
            div.Attributes.Add("id", "bodyContent");
            div.AddCssClass("p-5");
            if (!title.IsEmpty())
            {
                div.Attributes.Add("data-pjax-title", title);
            }
            if (!url.IsEmpty())
            {
                div.Attributes.Add("data-pjax-url", url);
            }
            if (!loadEvent.IsEmpty())
            {
                div.Attributes.Add("data-load-event", loadEvent);
            }
            if (!unloadEvent.IsEmpty())
            {
                div.Attributes.Add("data-unload-event", unloadEvent);
            }

            var writer = helper.ViewContext.Writer;
            div.RenderStartTag().WriteTo(writer, HtmlEncoder.Default);
            return new BodyContent(writer);
        }

        public static IHtmlContent Breadcrumbs(this IHtmlHelper helper, List<Breadcrumb> breadcrumbs)
        {
            var ul = new TagBuilder("ul");
            ul.AddCssClass("breadcrumb");
            breadcrumbs = breadcrumbs.Prepend(new Breadcrumb(Core.Dashboard, "Index", "Dashboard")).ToList();
            breadcrumbs.Each(x => {
                var li = new TagBuilder("li");
                li.AddCssClass("breadcrumb-item");
                li.InnerHtml.AppendHtml(AuthorizedActionLink(helper, x.Label, x.Action, x.Controller, x.RouteValues, returnEmpty: false, hasAccess: x.HasAccess));
                ul.InnerHtml.AppendHtml(li);
            });

            var last = breadcrumbs.Last();
            ul.MergeAttribute("data-pjax-title", last.Label);
            helper.ViewBag.Title = last.Label;
            var urlHelper = new UrlHelper(helper.ViewContext);
            ul.MergeAttribute("data-pjax-url", urlHelper.Action(last.Action, last.Controller, last.RouteValues));

            var divider = new TagBuilder("div");
            divider.AddCssClass("divider");

            var div = new TagBuilder("div");
            div.InnerHtml.AppendHtml(ul);
            div.InnerHtml.AppendHtml(divider);
            return div;
        }

        public static IHtmlContent FormButtons(this IHtmlHelper helper, string submitLabel = null, IHtmlContent extraButtons = null)
        {
            var submit = new TagBuilder("input");
            submit.MergeAttribute("type", "submit");
            submit.MergeAttribute("value", submitLabel ?? Core.Save);
            submit.AddCssClass("btn btn-primary");
            var span = new TagBuilder("span");
            span.AddCssClass("form-buttons col-8 col-ml-auto");
            span.InnerHtml.AppendHtml(submit);
            if (extraButtons != null)
            {
                span.InnerHtml.AppendHtml(extraButtons);
            }

            var columnDiv = new TagBuilder("div");
            columnDiv.AddCssClass("columns");
            columnDiv.InnerHtml.AppendHtml(span);

            var colDiv = new TagBuilder("div");
            colDiv.AddCssClass("col-6");
            colDiv.InnerHtml.AppendHtml(columnDiv);

            var div = new TagBuilder("div");
            div.AddCssClass("columns pt-5");
            div.InnerHtml.AppendHtml(colDiv);
            return div;
        }

        public static IDisposable BeginToolbar(this IHtmlHelper helper)
        {
            var writer = helper.ViewContext.Writer;
            writer.Write("<div class=\"col-12\"><div class=\"text-right\">");
            return new Toolbar(writer);
        }

        public static Dictionary<string, object> Classes(params DashClasses[] classList)
        {
            var dict = new Dictionary<string, object>();
            var list = classList.ToList();
            if (Buttons.Any(x => list.Contains(x)))
            {
                list.Add(DashClasses.Btn);
            }
            if (Dialogs.Any(x => list.Contains(x)))
            {
                list.Add(DashClasses.DashAjax);
            }
            dict.Add("class", list.ToCssClassList());
            return dict;
        }

        public static IHtmlContent ControlLabel(this IHtmlHelper helper, string forControl, string text, object htmlAttributes = null)
        {
            var htmlAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            htmlAttr["class"] = MergedList(htmlAttr.ContainsKey("class") ? htmlAttr["class"] : "", new[] { "col-4", "form-label" }).Combine();
            return helper.Label(forControl, text, htmlAttr);
        }

        public static IHtmlContent CustomCheckbox(this IHtmlHelper helper, string inputName, bool isChecked, string value, string display, string inputId = null, bool isFullWidth = true, bool isMultiple = false)
        {
            inputId = inputId ?? $"{inputName}_{value}";
            var input = new TagBuilder("input");
            input.Attributes.Add("type", "checkbox");
            input.Attributes.Add("name", inputName);
            input.Attributes.Add("value", value);
            if (isChecked)
            {
                input.Attributes.Add("checked", "true");
            }
            if (!inputId.IsEmpty())
            {
                input.Attributes.Add("id", inputId);
            }
            if (isMultiple)
            {
                input.AddCssClass("custom-control-input-multiple");
            }

            var label = new TagBuilder("label");
            label.AddCssClass("form-checkbox");
            label.Attributes.Add("for", inputId);
            var i = new TagBuilder("i");
            i.AddCssClass("form-icon");
            label.InnerHtml.AppendHtml(input);
            label.InnerHtml.AppendHtml(i);
            label.InnerHtml.Append(display);

            var div = new TagBuilder("div");
            div.AddCssClass("form-group");
            div.InnerHtml.AppendHtml(label);

            if (!isFullWidth)
            {
                return div;
            }

            var outerDiv = new TagBuilder("div");
            outerDiv.AddCssClass("col-12");
            outerDiv.InnerHtml.AppendHtml(div);
            return outerDiv;
        }

        public static IHtmlContent Help<TModel>(this IHtmlHelper<TModel> helper, string modelName, string fieldName, bool useInputGroup = true)
        {
            if (!helper.ViewContext.HttpContext.WantsHelp())
            {
                return HtmlString.Empty;
            }

            var key = (modelName != null ? modelName.Split(new[] { '.' }).Last() : typeof(TModel).Name) + "_" + fieldName;
            var resourceLib = new ResourceDictionary("ContextHelp");
            if (resourceLib.ContainsKey($"{key}") != true)
            {
                return HtmlString.Empty;
            }

            var icon = helper.Icon("help", false);
            var button = new TagBuilder("button");
            button.AddCssClass("btn btn-secondary");
            button.MergeAttribute("type", "button");
            button.MergeAttribute("role", "button");
            button.MergeAttribute("data-toggle", "context-help");
            button.MergeAttribute("data-message", resourceLib[$"{key}"].Replace("\"", "&quot;"));
            button.InnerHtml.AppendHtml(icon);
            if (useInputGroup)
            {
                var span = new TagBuilder("span");
                span.AddCssClass("input-group-custom input-group-addon");
                span.InnerHtml.AppendHtml(button);
                return span;
            }
            return button;
        }

        public static IHtmlContent HelpFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression, bool useInputGroup = true)
        {
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(expression, helper.ViewData, helper.MetadataProvider);
            return Help(helper, explorer.Metadata.ContainerType.Name, explorer.Metadata.PropertyName, useInputGroup);
        }

        public static TagBuilder Icon(this IHtmlHelper helper, string icon, bool large = true, bool primary = false)
        {
            var i = new TagBuilder("i");
            i.AddCssClass("dash");
            if (primary)
            {
                i.AddCssClass("text-primary");
            }
            if (large)
            {
                i.AddCssClass("dash-lg");
            }
            i.AddCssClass("dash-" + icon);
            return i;
        }

        public static IHtmlContent InputGroupButton<TModel>(this IHtmlHelper<TModel> helper, string targetId, IHtmlContent buttonLabel, List<string> itemList)
        {
            var button = new TagBuilder("button");
            button.AddCssClass("btn btn-secondary dropdown-toggle");
            button.Attributes["type"] = "button";
            button.Attributes["data-toggle"] = "dropdown";
            button.InnerHtml.AppendHtml(buttonLabel);

            var dropdownDiv = new TagBuilder("div");
            dropdownDiv.AddCssClass("dropdown dropdown-right");
            var ul = new TagBuilder("ul");
            ul.AddCssClass("menu");
            foreach (var item in itemList)
            {
                var itemTag = new TagBuilder("li");
                itemTag.AddCssClass("menu-item c-hand");
                itemTag.Attributes["data-toggle"] = "input-replace";
                itemTag.Attributes["data-target"] = $"#{targetId}";
                itemTag.Attributes["data-value"] = item;
                itemTag.InnerHtml.AppendHtml(item);
                ul.InnerHtml.AppendHtml(itemTag);
            }
            dropdownDiv.InnerHtml.AppendHtml(button);
            dropdownDiv.InnerHtml.AppendHtml(ul);

            var groupDiv = new TagBuilder("div");
            groupDiv.AddCssClass("input-group-custom input-group-addon");
            groupDiv.InnerHtml.AppendHtml(dropdownDiv);
            return groupDiv;
        }

        public static bool IsChecked<TModel, TProperty>(this IHtmlHelper<TModel> helper, IEnumerable<TProperty> list, Func<TProperty, bool> expression, int[] viewList, int value)
        {
            return (list != null && list.Any(expression)) || (viewList != null && viewList.Contains(value));
        }

        public static IHtmlContent LabelCheckBoxFor<TModel>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, bool>> expression, object htmlAttributes = null, int labelWidth = 4, int inputWidth = 8)
        {
            // load up the metadata we need
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(expression, helper.ViewData, helper.MetadataProvider);
            var fieldName = explorer.Metadata.PropertyName ?? ExpressionHelper.GetExpressionText(expression).Split(new[] { '.' }).Last();

            // build input
            var input = new TagBuilder("input");
            input.MergeAttributes(helper.InputAttributesFor(expression, htmlAttributes));
            input.Attributes.Remove("required");
            input.Attributes["type"] = "checkbox";
            input.Attributes["name"] = fieldName;
            input.Attributes["id"] = fieldName;
            input.Attributes["value"] = "true";
            if ((bool)explorer.Model)
            {
                input.Attributes["checked"] = "checked";
            }

            var checkboxDiv = new TagBuilder("div");
            checkboxDiv.AddCssClass("form-group");
            var i = new TagBuilder("i");
            i.AddCssClass("form-icon");
            var checkboxLabel = new TagBuilder("label");
            checkboxLabel.AddCssClass("form-checkbox");
            checkboxLabel.Attributes.Add("for", fieldName);
            checkboxLabel.InnerHtml.AppendHtml(input);
            checkboxLabel.InnerHtml.AppendHtml(i);
            checkboxLabel.InnerHtml.Append(explorer.Metadata.GetDisplayName() ?? fieldName);
            checkboxDiv.InnerHtml.AppendHtml(checkboxLabel);

            var rowDiv = new TagBuilder("div");
            rowDiv.AddCssClass("col-12");
            rowDiv.InnerHtml.AppendHtml(checkboxDiv);
            rowDiv.InnerHtml.AppendHtml(helper.HelpFor(expression, false));

            var innerDiv = new TagBuilder("div");
            innerDiv.AddCssClass("col-" + inputWidth);
            innerDiv.InnerHtml.AppendHtml(rowDiv);

            // put it all together
            var formGroup = FormGroup();
            var label = helper.LabelFor(expression, new { @class = "form-label col-" + labelWidth });
            formGroup.InnerHtml.AppendHtml(label);
            formGroup.InnerHtml.AppendHtml(innerDiv);

            return formGroup;
        }

        public static IHtmlContent LabelDropDownListFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression, IEnumerable<SelectListItem> listItems,
            object htmlAttributes = null, int labelWidth = 4, int inputWidth = 8, bool disabled = false, bool includeEmptyItem = true)
        {
            // build input
            var attrs = helper.InputAttributesFor(expression, htmlAttributes);
            if (disabled)
            {
                attrs.Add("disabled", "disabled");
            }
            attrs["class"] = MergedList(attrs.ContainsKey("class") ? attrs["class"] : "", new[] { "form-select" }).Combine();

            var input = helper.DropDownListFor(expression, listItems, includeEmptyItem ? "" : null, attrs);
            var innerDiv = helper.InputDiv(expression, inputWidth, input);
            var formGroup = FormGroup();
            formGroup.InnerHtml.AppendHtml(helper.StyledLabelFor(expression, attrs, labelWidth));
            formGroup.InnerHtml.AppendHtml(innerDiv);
            return formGroup;
        }

        public static TagBuilder LabelInputFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression,
            object inputAttributes = null, int labelWidth = 4, int inputWidth = 8, InputFieldType inputType = InputFieldType.Text, IHtmlContent groupAddOn = null)
        {
            var attrs = helper.InputAttributesFor(expression, inputAttributes);
            // any input type other than date can use the html5 input type
            attrs["type"] = inputType.ToString();

            // build input
            var input = helper.TextBoxFor(expression, attrs);
            var innerDiv = helper.InputDiv(expression, inputWidth, input, groupAddOn);
            var formGroup = FormGroup();
            formGroup.InnerHtml.AppendHtml(helper.StyledLabelFor(expression, attrs, labelWidth));
            formGroup.InnerHtml.AppendHtml(innerDiv);
            return formGroup;
        }

        public static TagBuilder LabelPasswordFor<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, object inputAttributes = null, int labelWidth = 4, int inputWidth = 8)
        {
            var attrs = helper.InputAttributesFor(expression, inputAttributes);
            var input = helper.PasswordFor(expression, attrs);
            var innerDiv = helper.InputDiv(expression, inputWidth, input);
            var formGroup = FormGroup();
            formGroup.InnerHtml.AppendHtml(helper.StyledLabelFor(expression, attrs, labelWidth));
            formGroup.InnerHtml.AppendHtml(innerDiv);
            return formGroup;
        }

        public static IHtmlContent LabelTextAreaFor<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, object inputAttributes = null, int labelWidth = 4, int inputWidth = 8)
        {
            var attrs = helper.InputAttributesFor(expression, inputAttributes);
            var input = helper.TextAreaFor(expression, attrs);
            var innerDiv = helper.InputDiv(expression, inputWidth, input);
            var formGroup = FormGroup();
            formGroup.InnerHtml.AppendHtml(helper.StyledLabelFor(expression, attrs, labelWidth));
            formGroup.InnerHtml.AppendHtml(innerDiv);
            return formGroup;
        }

        public static IHtmlContent ToCard(this IHtmlHelper helper, string cardText, CardTypes cardType = CardTypes.Primary)
        {
            var inner = new TagBuilder("div");
            inner.AddCssClass("card-body");
            inner.InnerHtml.AppendHtml(cardText);

            var div = new TagBuilder("div");
            div.AddCssClass("card text-white m-2 bg-" + cardType.ToString().ToLower());
            div.InnerHtml.AppendHtml(inner);

            var outerDiv = new TagBuilder("div");
            outerDiv.AddCssClass("col-12");
            outerDiv.InnerHtml.AppendHtml(div);
            return outerDiv;
        }

        public static List<SelectListItem> ToSelectList<T>(this IEnumerable<T> enumerable, Func<T, string> text, Func<T, string> value)
        {
            return enumerable.Select(f => new SelectListItem { Text = text(f), Value = value(f) }).ToList();
        }

        private static string Combine(this IEnumerable<string> list, string separator = " ")
        {
            return String.Join(separator, list);
        }

        private static TagBuilder FormGroup()
        {
            var formGroup = new TagBuilder("div");
            formGroup.AddCssClass("form-group");
            return formGroup;
        }

        private static IDictionary<string, object> InputAttributesFor<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes = null)
        {
            var attrs = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes).ToDictionary(x => x.Key, x => x.Value);
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(expression, helper.ViewData, helper.MetadataProvider);

            var validator = new ClientValidatorCache();
            var optionsAccessor = (IOptions<MvcViewOptions>)helper.ViewContext.HttpContext.RequestServices.GetService(typeof(IOptions<MvcViewOptions>));
            var clientValidatorProviders = optionsAccessor.Value.ClientModelValidatorProviders;
            var clientModelValidatorProvider = new CompositeClientModelValidatorProvider(clientValidatorProviders);
            validator.GetValidators(explorer.Metadata, clientModelValidatorProvider).Each(x => {
                if (x is StringLengthAttribute stringLengthAttribute && stringLengthAttribute.MaximumLength > 0)
                {
                    attrs.Append("maxlength", stringLengthAttribute.MaximumLength.ToString());
                }
                if (x is MinLengthAttribute minLengthAttribute && minLengthAttribute.Length > 0)
                {
                    attrs.Append("data-minlength", minLengthAttribute.Length.ToString());
                }
            });
            if (explorer.Metadata.DataTypeName != null)
            {
                attrs.Append("type", explorer.Metadata.DataTypeName == "EmailAddress" ? "Email" : explorer.Metadata.DataTypeName);
            }
            if (explorer.Metadata.IsRequired)
            {
                attrs.Append("required", "true");
            }
            attrs.Append("class", "form-input");

            return attrs;
        }

        private static TagBuilder InputDiv<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, int inputWidth = 8, IHtmlContent input = null, IHtmlContent groupAddOn = null)
        {
            var innerDiv = new TagBuilder("div");
            innerDiv.AddCssClass("col-" + inputWidth);
            var help = helper.HelpFor(expression);
            if (help == HtmlString.Empty && groupAddOn == null)
            {
                innerDiv.InnerHtml.AppendHtml(input);
            }
            else
            {
                var inputGroup = new TagBuilder("div");
                inputGroup.AddCssClass("input-group");
                inputGroup.InnerHtml.AppendHtml(input);
                inputGroup.InnerHtml.AppendHtml(groupAddOn);
                inputGroup.InnerHtml.AppendHtml(help);
                innerDiv.InnerHtml.AppendHtml(inputGroup);
            }
            return innerDiv;
        }

        private static bool IsRequired(ModelExplorer explorer, IDictionary<string, object> attrs)
        {
            return explorer.Metadata.IsRequired ? true : (attrs != null && attrs.ContainsKey("required") && attrs["required"].ToString().ToLower() == "true");
        }

        private static void Merge(this List<string> list, string item)
        {
            if (!list.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(item);
            }
        }

        private static List<string> MergedList(object classList, IEnumerable<string> classes = null)
        {
            var newList = new List<string>();
            if (classList != null && !classList.ToString().IsEmpty())
            {
                newList = classList.ToString().Split(' ').ToList();
            }
            if (classes != null)
            {
                classes.ToList().ForEach(x => newList.Merge(x));
            }
            return newList;
        }

        private static IHtmlContent StyledLabelFor<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes = null, int labelWidth = 4)
        {
            var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, helper.ViewData, helper.MetadataProvider);
            var label = helper.LabelFor(expression, new { @class = "form-label col-" + labelWidth + (IsRequired(metadata, htmlAttributes) ? " required" : "") });
            return label;
        }

        private class Toolbar : IDisposable
        {
            private readonly TextWriter _writer;

            public Toolbar(TextWriter writer)
            {
                _writer = writer;
            }

            public void Dispose()
            {
                _writer.Write("</div></div>");
            }
        }
        private class BodyContent : IDisposable
        {
            private readonly TextWriter _writer;

            public BodyContent(TextWriter writer)
            {
                _writer = writer;
            }

            public void Dispose()
            {
                _writer.Write("</div>");
            }
        }
    }
}
