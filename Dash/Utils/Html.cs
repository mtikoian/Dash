using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Dash
{
    public enum BootstrapTypes
    {
        Success,
        Warning,
        Info,
        Danger,
        Primary
    }

    public enum HttpVerbs
    {
        Get,
        Put,
        Post,
        Delete
    }

    public enum RngnClasses
    {
        Btn,
        BtnSm,
        BtnSuccess,
        BtnWarning,
        BtnInfo,
        BtnDanger,
        BtnPrimary,
        BtnLink,
        BtnSecondary,
        RngnAjax,
        RngnConfirm,
        RngnDialog,
        RngnPrompt
    }

    /// <summary>
    /// Collection of extension methods to provide common functions.
    /// </summary>
    public static class Html
    {
        private static List<RngnClasses> Buttons = new List<RngnClasses> { RngnClasses.BtnDanger, RngnClasses.BtnInfo, RngnClasses.BtnPrimary,
            RngnClasses.BtnSuccess, RngnClasses.BtnWarning, RngnClasses.BtnLink };
        private static List<RngnClasses> Dialogs = new List<RngnClasses> { RngnClasses.RngnConfirm, RngnClasses.RngnDialog, RngnClasses.RngnPrompt };

        public enum InputFieldType { Text, Email, Number, DateTime, Date, Tel, Password }

        /// <summary>
        /// Return a link if the user has access.
        /// </summary>
        /// <param name="helper">HTML view helper</param>
        /// <param name="linkText">Link text</param>
        /// <param name="action">Action</param>
        /// <param name="controller">Controller</param>
        /// <param name="routeValues">Route values</param>
        /// <param name="htmlAttributes">HTML attributes</param>
        /// <param name="hasAccess">Does user have access</param>
        /// <returns></returns>
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
                return new HtmlString(span.ToString());
            }
        }

        /// <summary>
        /// Create a button if the user is authorized. Alias that doesn't use routeValues.
        /// </summary>
        /// <param name="helper">HTML view helper</param>
        /// <param name="text">Button text</param>
        /// <param name="controller">Controller for the link</param>
        /// <param name="action">Action for the link</param>
        /// <param name="btnType">Bootstrap button type</param>
        /// <param name="ajaxType">If ajax, dialog type</param>
        /// <param name="htmlAttributes">Attributes for button.</param>
        /// <param name="hasAccess">User has access</param>
        /// <returns>Returns the HTML for the button if authorized, else an empty string.</returns>
        public static HtmlString AuthorizedButton(this IHtmlHelper helper, string text, string controller, string action, RngnClasses? btnType = null, 
            RngnClasses? ajaxType = null, object htmlAttributes = null, bool hasAccess = true)
        {
            return AuthorizedButton(helper, text, controller, action, null, btnType, ajaxType, htmlAttributes, hasAccess);
        }

        /// <summary>
        /// Create a button if the user is authorized.
        /// </summary>
        /// <param name="helper">HTML view helper</param>
        /// <param name="text">Button text</param>
        /// <param name="controller">Controller for the link</param>
        /// <param name="action">Action for the link</param>
        /// <param name="routeValues">Params for the link.</param>
        /// <param name="btnType">Bootstrap button type</param>
        /// <param name="ajaxType">If ajax, dialog type</param>
        /// <param name="htmlAttributes">Attributes for button.</param>
        /// <returns>Returns the HTML for the button if authorized, else an empty string.</returns>
        public static HtmlString AuthorizedButton(this IHtmlHelper helper, string text, string controller, string action, object routeValues, RngnClasses? btnType = null, 
            RngnClasses? ajaxType = null, object htmlAttributes = null, bool hasAccess = true)
        {
            if (!hasAccess)
            {
                return HtmlString.Empty;
            }

            var htmlAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            var classList = new List<string>();
            classList.Merge("btn");
            classList.Merge("mr-1");
            classList.Merge((btnType ?? RngnClasses.BtnPrimary).ToCssClass());
            if (ajaxType.HasValue)
            {
                classList.Merge(RngnClasses.RngnAjax.ToCssClass());
                classList.Merge(ajaxType.Value.ToCssClass());
            }
            htmlAttr["class"] = classList.Combine();

            var urlHelper = new UrlHelper(helper.ViewContext);
            htmlAttr["data-href"] = urlHelper.Action(action, controller, new RouteValueDictionary(routeValues));
            htmlAttr["type"] = "button";
            htmlAttr["role"] = "button";

            var btn = new TagBuilder("button");
            btn.MergeAttributes(htmlAttr);
            btn.InnerHtml.Append(text);
            return new HtmlString(btn.ToString());
        }

        /// <summary>
        /// Return a link if the user has access.
        /// </summary>
        /// <param name="helper">HTML view helper</param>
        /// <param name="linkText">Link text</param>
        /// <param name="action">Action</param>
        /// <param name="controller">Controller</param>
        /// <param name="icon">Icon for the menu item</param>
        /// <param name="authorized">True if user is authorized</param>
        /// <returns>Returns the HTML for the menu link.</returns>
        public static HtmlString AuthorizedMenu(this IHtmlHelper helper, string linkText, string action, string controller, string icon, bool authorized)
        {
            if (!authorized)
            {
                return HtmlString.Empty;
            }

            var a = new TagBuilder("a");
            a.AddCssClass(Classes(RngnClasses.RngnDialog)["class"].ToString());

            var urlHelper = new UrlHelper(helper.ViewContext);
            a.Attributes.Add("href", urlHelper.Action(action, controller));
            a.Attributes.Add("title", linkText);
            var span = new TagBuilder("span");
            span.InnerHtml.Append(linkText);
            a.InnerHtml.AppendHtml(helper.Icon(icon));
            a.InnerHtml.Append(" ");
            a.InnerHtml.AppendHtml(span);
            var li = new TagBuilder("li");
            li.InnerHtml.AppendHtml(a);
            return new HtmlString(li.ToString());
        }

        /// <summary>
        /// Start a form tag.
        /// </summary>
        /// <param name="helper">HTML view helper</param>
        /// <param name="action">Action</param>
        /// <param name="controller">Controller</param>
        /// <param name="routeValues">Route values to use when building URL.</param>
        /// <param name="title">Title of the form dialog.</param>
        /// <param name="dataEvent">Javascript event to fire after saving the form.</param>
        /// <param name="dataUrl">URL to get data for the form from the server.</param>
        /// <param name="htmlAttributes">HTML attributes for the form.</param>
        /// <param name="ajaxForm">Is this form an ajax submission.</param>
        /// <returns>Return the wrapper for the form content.</returns>
        public static MvcForm BeginBootForm(this IHtmlHelper helper, string action, string controller, object routeValues = null, string title = null, string dataEvent = null, string dataUrl = null, object htmlAttributes = null, bool ajaxForm = true, HttpVerbs method = HttpVerbs.Post)
        {
            var htmlAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            htmlAttr["class"] = MergedList(htmlAttr["class"], new string[] { (ajaxForm ? "rngn-form" : ""), "pt-1 mx-1 row" }).Combine();
            htmlAttr["data-toggle"] = MergedList(htmlAttr["data-toggle"], new[] { "validator" }).Combine();
            if (!title.IsEmpty())
            {
                htmlAttr["data-title"] = title.Trim();
            }
            if (!htmlAttr.ContainsKey("id"))
            {
                htmlAttr["id"] = $"{action.ToLower()}{controller.UppercaseFirst()}Form";
            }
            if (!htmlAttr.ContainsKey("data-event") && !dataEvent.IsEmpty())
            {
                htmlAttr["data-event"] = dataEvent;
            }
            if (!htmlAttr.ContainsKey("data-url") && !dataUrl.IsEmpty())
            {
                htmlAttr["data-url"] = dataUrl;
            }
            if (method != HttpVerbs.Post && method != HttpVerbs.Get)
            {
                htmlAttr["data-method"] = method;
            }

            var mvcForm = helper.BeginForm(action, controller, new RouteValueDictionary(routeValues), FormMethod.Post, true, htmlAttr);
            helper.ViewContext.Writer.Write(helper.AntiForgeryToken());
            return mvcForm;
        }

        /// <summary>
        /// Start a bootstrap toolbar.
        /// </summary>
        /// <param name="html">HTML view helper</param>
        /// <returns>Returns the wrapper for the toolbar content.</returns>
        public static IDisposable BeginToolbar(this IHtmlHelper helper)
        {
            var writer = helper.ViewContext.Writer;
            writer.Write("<div class=\"p-2 d-flex\"><div class=\"ml-auto\">");
            return new Toolbar(writer);
        }

        /// <summary>
        /// Make a dictionary of attributes using a class list to start.
        /// </summary>
        /// <param name="classes">List of RngnClasses.</param>
        /// <returns>Returns a dictionary with one element named class.</returns>
        public static Dictionary<string, object> Classes(params RngnClasses[] classList)
        {
            var dict = new Dictionary<string, object>();
            var list = classList.ToList();
            if (Buttons.Any(x => list.Contains(x)))
            {
                list.Add(RngnClasses.Btn);
            }
            if (Dialogs.Any(x => list.Contains(x)))
            {
                list.Add(RngnClasses.RngnAjax);
            }
            dict.Add("class", list.ToCssClassList());
            return dict;
        }

        /// <summary>
        /// Make a form control label.
        /// </summary>
        /// <param name="helper">HTML helper</param>
        /// <param name="forControl">Name of the control the label belongs to.</param>
        /// <param name="text">Text of the label.</param>
        /// <param name="htmlAttributes">HTML attributes for the control.</param>
        /// <returns>Returns the HTML for the label.</returns>
        public static IHtmlContent ControlLabel(this IHtmlHelper helper, string forControl, string text, object htmlAttributes = null)
        {
            var htmlAttr = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            htmlAttr["class"] = MergedList(htmlAttr["class"], new[] { "col-4", "col-form-label" }).Combine();
            return helper.Label(forControl, text, htmlAttr);
        }

        /// <summary>
        /// Make a bootstrap v4 custom checkbox control.
        /// </summary>
        /// <param name="helper">HTML helper</param>
        /// <param name="inputName">Name for the input tag</param>
        /// <param name="isChecked">Is the input checked</param>
        /// <param name="value">Value for the input</param>
        /// <param name="display">Text to display next to the check box</param>
        /// <param name="inputId">ID of the input tag</param>
        /// <param name="isFullWidth">Wrap the checkbox in a col-12 div if true.</param>
        /// <returns>Returns the HTML snippet for the checkbox.</returns>
        public static HtmlString CustomCheckbox(this IHtmlHelper helper, string inputName, bool isChecked, string value, string display, string inputId = null, bool isFullWidth = true, bool isMultiple = false)
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
            input.AddCssClass("custom-control-input");
            if (isMultiple)
            {
                input.AddCssClass("custom-control-input-multiple");
            }

            var label = new TagBuilder("label");
            label.AddCssClass("custom-control-label");
            label.Attributes.Add("for", inputId);
            label.InnerHtml.Append(display);

            var div = new TagBuilder("div");
            div.AddCssClass("custom-control custom-checkbox");
            div.InnerHtml.AppendHtml(input);
            div.InnerHtml.AppendHtml(label);

            if (!isFullWidth)
            {
                return new HtmlString(div.ToString());
            }

            var outerDiv = new TagBuilder("div");
            outerDiv.AddCssClass("col-12");
            outerDiv.InnerHtml.AppendHtml(div);
            return new HtmlString(outerDiv.ToString());
        }

        /// <summary>
        /// Sanitize a string to use within a javascript string.
        /// </summary>
        /// <param name="helper">HTML view helper</param>
        /// <param name="value">String to escape</param>
        /// <returns>Escaped string.</returns>
        public static HtmlString EscapeForJs(this IHtmlHelper helper, string value)
        {
            return new HtmlString($"'{value.Replace("'", "\'")}'");
        }

        /// <summary>
        /// Builds the HTML tag for a contexthelp link.
        /// </summary>
        /// <typeparam name="TModel">Model to build help for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="modelName">Name of model to get help for.</param>
        /// <param name="fieldName">Name of a field to get help for.</param>
        /// <param name="useInputGroup">Wrap icon in an input-group.</param>
        /// <param name="rightPad">Add right padding to icon.</param>
        /// <returns>Returns HTML if help is found for the model/field, else returns an empty string.</returns>
        public static HtmlString Help<TModel>(this IHtmlHelper<TModel> helper, string modelName, string fieldName, bool useInputGroup = true, bool rightPad = false)
        {
            if (!helper.ViewContext.HttpContext.Session.GetString("ContextHelp").ToBool())
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
            button.AddCssClass("btn btn-secondary rngn-context-help");
            button.Attributes.Add("type", "button");
            button.Attributes.Add("role", "button");
            button.Attributes["data-message"] = resourceLib[$"{key}"].Replace("\"", "&quot;");
            button.InnerHtml.AppendHtml(icon);
            if (rightPad)
            {
                button.AddCssClass("context-help-pad");
            }
            if (useInputGroup)
            {
                var span = new TagBuilder("span");
                span.AddCssClass("input-group-append");
                span.InnerHtml.AppendHtml(button);
                return new HtmlString(span.ToString());
            }
            return new HtmlString(button.ToString());
        }

        /// <summary>
        /// Builds the HTML tag for a contexthelp link.
        /// </summary>
        /// <typeparam name="TModel">Model to build help for.</typeparam>
        /// <typeparam name="TValue">Field name to build help for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model object</param>
        /// <param name="htmlAttributes">Any html attributes to add.</param>
        /// <returns>Returns HTML if help is found for the model/field, else returns an empty string.</returns>
        public static HtmlString HelpFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression, bool useInputGroup = true, bool rightPad = false)
        {
            var explorer = ExpressionMetadataProvider.FromLambdaExpression(expression, helper.ViewData, helper.MetadataProvider);
            return Help(helper, explorer.Metadata.ContainerType.Name, explorer.Metadata.PropertyName, useInputGroup, rightPad);
        }

        /// <summary>
        /// Create an icon tag.
        /// </summary>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="icon">Icon name</param>
        /// <param name="large">Make the icon larger</param>
        /// <returns>Returns an icon tag.</returns>
        public static TagBuilder Icon(this IHtmlHelper helper, string icon, bool large = true)
        {
            var i = new TagBuilder("i");
            i.AddCssClass("rn");
            if (large)
            {
                i.AddCssClass("rn-lg");
            }
            i.AddCssClass("rn-" + icon);
            return i;
        }

        /// <summary>
        /// Create the HTML for a input group dropdown.
        /// </summary>
        /// <typeparam name="TModel">Model for the view.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="targetId">Input ID that selected value should be saved to.</param>
        /// <param name="buttonLabel">Label for the button - can be text or an html string.</param>
        /// <param name="itemList">List of items for the dropdown.</param>
        /// <returns>Returns an input group button with a dropdown.</returns>
        public static HtmlString InputGroupButton<TModel>(this IHtmlHelper<TModel> helper, string targetId, string buttonLabel, List<string> itemList)
        {
            var button = new TagBuilder("button");
            button.AddCssClass("btn btn-secondary dropdown-toggle");
            button.Attributes["type"] = "button";
            button.Attributes["data-toggle"] = "dropdown";
            button.Attributes["aria-haspopup"] = "true";
            button.Attributes["aria-expanded"] = "false";
            button.InnerHtml.Append(buttonLabel);

            var dropdownDiv = new TagBuilder("div");
            dropdownDiv.AddCssClass("dropdown");
            var innerDiv = new TagBuilder("div");
            innerDiv.AddCssClass("dropdown-menu dropdown-menu-right");
            foreach (var item in itemList)
            {
                var itemTag = new TagBuilder("a");
                itemTag.AddCssClass("dropdown-item rngn-input-replace");
                itemTag.Attributes["data-target"] = targetId;
                itemTag.Attributes["data-value"] = item;
                itemTag.InnerHtml.AppendHtml(item);
                innerDiv.InnerHtml.AppendHtml(itemTag);
            }
            dropdownDiv.InnerHtml.AppendHtml(button);
            dropdownDiv.InnerHtml.AppendHtml(innerDiv);

            var groupDiv = new TagBuilder("div");
            groupDiv.AddCssClass("input-group-btn input-group-append");
            groupDiv.InnerHtml.AppendHtml(dropdownDiv);
            return new HtmlString(groupDiv.ToString());
        }

        /// <summary>
        /// Determine if a checkbox should be checked. Used with lists of IDs.
        /// </summary>
        /// <typeparam name="TModel">Model for the view.</typeparam>
        /// <typeparam name="TProperty">Property the checkboxes are for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="list">List of items to check for this value in.</param>
        /// <param name="expression">Expression to use to check the list for this value.</param>
        /// <param name="viewList">List of selected items from the viewbag.</param>
        /// <param name="value">Value for the current checkbox.</param>
        /// <returns></returns>
        public static bool IsChecked<TModel, TProperty>(this IHtmlHelper<TModel> helper, IEnumerable<TProperty> list, Func<TProperty, bool> expression, int[] viewList, int value)
        {
            return (list != null && list.Any(expression)) || (viewList != null && viewList.Contains(value));
        }

        /// <summary>
        /// Builds the HTML for label and checkbox.
        /// </summary>
        /// <typeparam name="TModel">Model to build checkbox for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model object</param>
        /// <param name="htmlAttributes">Any html attributes to add.</param>
        /// <param name="labelWidth">Width of the control label.</param>
        /// <param name="inputWidth">Width of the input.</param>
        /// <returns>Returns HTML for the label and checkbox.</returns>
        public static HtmlString LabelCheckBoxFor<TModel>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, bool>> expression, object htmlAttributes = null, int labelWidth = 4, int inputWidth = 8)
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
            input.AddCssClass("custom-control-input");

            var checkboxDiv = new TagBuilder("div");
            checkboxDiv.AddCssClass("custom-control custom-checkbox");
            var checkboxLabel = new TagBuilder("label");
            checkboxLabel.AddCssClass("custom-control-label");
            checkboxLabel.Attributes.Add("for", fieldName);
            checkboxLabel.InnerHtml.Append(explorer.Metadata.GetDisplayName() ?? fieldName);
            checkboxDiv.InnerHtml.AppendHtml(input);
            checkboxDiv.InnerHtml.AppendHtml(checkboxLabel);

            var rowDiv = new TagBuilder("div");
            rowDiv.AddCssClass("col-12");
            rowDiv.InnerHtml.AppendHtml(checkboxDiv);
            rowDiv.InnerHtml.AppendHtml(helper.HelpFor(expression, false, true));
            rowDiv.InnerHtml.AppendHtml(ErrorDiv());

            var innerDiv = new TagBuilder("div");
            innerDiv.AddCssClass("col-" + inputWidth);
            innerDiv.InnerHtml.AppendHtml(rowDiv);

            // put it all together
            var formGroup = FormGroup();
            var label = helper.LabelFor(expression, new { @class = "col-form-label col-" + labelWidth });
            formGroup.InnerHtml.AppendHtml(label);
            formGroup.InnerHtml.AppendHtml(innerDiv);

            return new HtmlString(formGroup.ToString());
        }

        /// <summary>
        /// Builds the HTML for label and dropdown.
        /// </summary>
        /// <typeparam name="TModel">Model to build help for.</typeparam>
        /// <typeparam name="TValue">Field name to build help for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model object</param>
        /// <param name="listItems">List of items for the dropdown.</param>
        /// <param name="htmlAttributes">Any html attributes to add.</param>
        /// <param name="labelWidth">Width of the control label.</param>
        /// <param name="inputWidth">Width of the dropdown.</param>
        /// <param name="disabled">Dropdown is disabled.</param>
        /// <param name="includeEmptyItem">Include an empty option at the top of the dropdown.</param>
        /// <returns>Returns HTML if help is found for the model/field, else returns an empty string.</returns>
        public static HtmlString LabelDropDownListFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression, IEnumerable<SelectListItem> listItems,
            object htmlAttributes = null, int labelWidth = 4, int inputWidth = 8, bool disabled = false, bool includeEmptyItem = true)
        {
            // build input
            var attrs = helper.InputAttributesFor(expression, htmlAttributes);
            if (disabled)
            {
                attrs.Add("disabled", "disabled");
            }
            attrs["class"] = MergedList(attrs["class"], new[] { "custom-select" }).Combine();

            var input = helper.DropDownListFor(expression, listItems, includeEmptyItem ? "" : null, attrs);
            var innerDiv = helper.InputDiv(expression, inputWidth, input);
            var formGroup = FormGroup();
            formGroup.InnerHtml.AppendHtml(helper.StyledLabelFor(expression, attrs, labelWidth));
            formGroup.InnerHtml.AppendHtml(innerDiv);
            return new HtmlString(formGroup.ToString());
        }

        /// <summary>
        /// Builds the HTML for a form group.
        /// </summary>
        /// <typeparam name="TModel">Model to build help for.</typeparam>
        /// <typeparam name="TValue">Field name to build help for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model expression</param>
        /// <param name="inputAttributes">Any html attributes to add</param>
        /// <param name="labelWidth">Bootstrap col width for label</param>
        /// <param name="inputWidth">Bootstrap col width for input</param>
        /// <param name="inputType">HTML5 input type</param>
        /// <returns>Returns HTML if help is found for the model/field, else returns an empty string.</returns>
        public static HtmlString LabelInputFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression,
            object inputAttributes = null, int labelWidth = 4, int inputWidth = 8, InputFieldType inputType = InputFieldType.Text, HtmlString groupAddOn = null)
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
            return new HtmlString(formGroup.ToString());
        }

        /// <summary>
        /// Builds the HTML for label and textbox.
        /// </summary>
        /// <typeparam name="TModel">Model to build help for.</typeparam>
        /// <typeparam name="TProperty">Field name to build help for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model object</param>
        /// <param name="inputAttributes">Any html attributes to add.</param>
        /// <param name="labelWidth">Bootstrap col width for label</param>
        /// <param name="inputWidth">Bootstrap col width for input</param>
        /// <returns>Returns HTML if help is found for the model/field, else returns an empty string.</returns>
        public static HtmlString LabelPasswordFor<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, object inputAttributes = null, int labelWidth = 4, int inputWidth = 8)
        {
            var attrs = helper.InputAttributesFor(expression, inputAttributes);
            var input = helper.PasswordFor(expression, attrs);
            var innerDiv = helper.InputDiv(expression, inputWidth, input);
            var formGroup = FormGroup();
            formGroup.InnerHtml.AppendHtml(helper.StyledLabelFor(expression, attrs, labelWidth));
            formGroup.InnerHtml.AppendHtml(innerDiv);
            return new HtmlString(formGroup.ToString());
        }

        /// <summary>
        /// Builds the HTML for a form group.
        /// </summary>
        /// <typeparam name="TModel">Model to build help for.</typeparam>
        /// <typeparam name="TProperty">Field name to build help for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model object</param>
        /// <param name="inputAttributes">Any html attributes to add.</param>
        /// <returns>Returns HTML if help is found for the model/field, else returns an empty string.</returns>
        public static HtmlString LabelTextAreaFor<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, object inputAttributes = null, int labelWidth = 4, int inputWidth = 8)
        {
            var attrs = helper.InputAttributesFor(expression, inputAttributes);
            var input = helper.TextAreaFor(expression, attrs);
            var innerDiv = helper.InputDiv(expression, inputWidth, input);
            var formGroup = FormGroup();
            formGroup.InnerHtml.AppendHtml(helper.StyledLabelFor(expression, attrs, labelWidth));
            formGroup.InnerHtml.AppendHtml(innerDiv);
            return new HtmlString(formGroup.ToString());
        }

        /// <summary>
        /// Builds a bootstrap card.
        /// </summary>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="cardText">Text of the card.</param>
        /// <param name="cardType">Bootstrap type for the card.</param>
        /// <returns>Returns HTML for the card.</returns>
        public static HtmlString ToCard(this IHtmlHelper helper, string cardText, BootstrapTypes cardType = BootstrapTypes.Primary)
        {
            var inner = new TagBuilder("div");
            inner.AddCssClass("card-body");
            inner.InnerHtml.Append(cardText);

            var div = new TagBuilder("div");
            div.AddCssClass("card text-white m-2 bg-" + cardType.ToString().ToLower());
            div.InnerHtml.AppendHtml(inner);

            var outerDiv = new TagBuilder("div");
            outerDiv.AddCssClass("col-12");
            outerDiv.InnerHtml.AppendHtml(div);
            return new HtmlString(outerDiv.ToString());
        }

        /// <summary>
        /// Convert an enumerable to a list of select list items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">Enumerable to convert.</param>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <returns>List of SelectListitem.</returns>
        public static List<SelectListItem> ToSelectList<T>(this IEnumerable<T> enumerable, Func<T, string> text, Func<T, string> value)
        {
            return enumerable.Select(f => new SelectListItem { Text = text(f), Value = value(f) }).ToList();
        }

        /// <summary>
        /// Convert a list of strings into a single string.
        /// </summary>
        /// <param name="list">List to combine.</param>
        /// <param name="separator">Separator to use.</param>
        /// <returns>Returns a single string of items separated by separator.</returns>
        private static string Combine(this IEnumerable<string> list, string separator = " ")
        {
            return String.Join(separator, list);
        }

        /// <summary>
        /// Build the div to contain error messages for an input.
        /// </summary>
        /// <returns>HTML snippet.</returns>
        private static string ErrorDiv()
        {
            var errorDiv = new TagBuilder("div");
            errorDiv.AddCssClass("help-block with-errors");
            return errorDiv.ToString();
        }

        /// <summary>
        /// Build the div to contain a form control and input.
        /// </summary>
        /// <returns>HTML snippet.</returns>
        private static TagBuilder FormGroup()
        {
            var formGroup = new TagBuilder("div");
            formGroup.AddCssClass("form-group row");
            return formGroup;
        }

        /// <summary>
        /// Build a complete set of needed attributes based on metadata in the model.
        /// </summary>
        /// <typeparam name="TModel">Model to build attributes for.</typeparam>
        /// <typeparam name="TProperty">Field name to build attributes for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model object</param>
        /// <param name="htmlAttributes">Any html attributes to add.</param>
        /// <returns>Returns a dictionary of attributes.</returns>
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
            if (attrs.ContainsKey("required") && attrs["required"].ToString().ToLower() == "false")
            {
                attrs.Remove("required");
            }
            attrs.Append("class", "form-control");

            return attrs;
        }

        /// <summary>
        /// Build the div containing the input, help, and error messages if any.
        /// </summary>
        /// <typeparam name="TModel">Model to build help for.</typeparam>
        /// <typeparam name="TProperty">Field name to build help for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Expression to get the value for the input.</param>
        /// <param name="inputWidth">Bootstrap col width for input.</param>
        /// <param name="input">Input tag snippet.</param>
        /// <param name="groupAddOn">Input group addon snippet.</param>
        /// <returns>Returns a div for the input.</returns>
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
            innerDiv.InnerHtml.AppendHtml(ErrorDiv());
            return innerDiv;
        }

        /// <summary>
        /// Check if a field is required based on the attrs and model metadata.
        /// </summary>
        /// <param name="explorer">Model explorer</param>
        /// <param name="attrs">HTML attributes to check.</param>
        /// <returns>Returns true if this field is required.</returns>
        private static bool IsRequired(ModelExplorer explorer, IDictionary<string, object> attrs)
        {
            return explorer.Metadata.IsRequired ? true : (attrs != null && attrs.ContainsKey("required") && attrs["required"].ToString().ToLower() == "true");
        }

        /// <summary>
        /// Add the item to the list if it doesn't already exist.
        /// </summary>
        /// <param name="list">List of strings to add to.</param>
        /// <param name="item">Item to add.</param>
        private static void Merge(this List<string> list, string item)
        {
            if (!list.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(item);
            }
        }

        /// <summary>
        /// Split classList into a list and merge all of the items in classes.
        /// </summary>
        /// <param name="classList">Object(string) to split on spaces.</param>
        /// <param name="classes">List of new classes to merge into the list.</param>
        /// <returns>Returns a list of classes without duplicates.</returns>
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

        /// <summary>
        /// Build a label for an input.
        /// </summary>
        /// <typeparam name="TModel">Model to build label for.</typeparam>
        /// <typeparam name="TProperty">Field name to build label for.</typeparam>
        /// <param name="helper">IHtmlHelper object this method extends.</param>
        /// <param name="expression">Model object</param>
        /// <param name="htmlAttributes">Any html attributes to add.</param>
        /// <param name="labelWidth">Width of the label.</param>
        /// <returns></returns>
        private static string StyledLabelFor<TModel, TProperty>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes = null, int labelWidth = 4)
        {
            var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, helper.ViewData, helper.MetadataProvider);
            var label = helper.LabelFor(expression, new { @class = "col-form-label col-" + labelWidth + (IsRequired(metadata, htmlAttributes) ? " required" : "") });
            return label.ToString();
        }

        /// <summary>
        /// Class for writing out the bootstrap toolbar.
        /// </summary>
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
    }
}