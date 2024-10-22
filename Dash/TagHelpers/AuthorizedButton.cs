﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class AuthorizedButtonTagHelper : BaseTagHelper
    {
        IHttpContextAccessor _HttpContextAccessor;

        public AuthorizedButtonTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper) => _HttpContextAccessor = httpContextAccessor;

        public string Action { get; set; }
        public DashButtons Class { get; set; } = DashButtons.BtnPrimary;
        public string Controller { get; set; }
        public bool ForceReload { get; set; } = false;
        public bool? HasAccess { get; set; }
        public string Role { get; set; }
        public object RouteValues { get; set; }
        public string Target { get; set; }
        public string Title { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            output.TagMode = TagMode.StartTagAndEndTag;
            if (!(HasAccess ?? _HttpContextAccessor.HttpContext.User.HasAccess(Controller, Action, HttpVerbs.Get)))
            {
                NoRender = true;
                base.Process(context, output);
                return;
            }

            output.TagName = "a";
            var urlHelper = new UrlHelper(HtmlHelper.ViewContext);
            output.Attributes.Add("href", urlHelper.Action(Action, Controller, RouteValues));
            output.Attributes.Add("data-method", "GET");
            output.Attributes.Add("title", Title);
            output.Attributes.AddIf("target", Target, !Target.IsEmpty());
            output.Attributes.AddIf("role", Role, !Role.IsEmpty());
            output.Attributes.AddIf("data-reload", "true", ForceReload);
            output.Content.Append(Title);

            var classList = new List<string> { "btn", "mr-2" };
            if (!Target.IsEmpty())
                classList.Add("pjax-no-follow");
            classList.Add(Class.ToCssClass());
            output.Attributes.Add("class", classList.Join(" "));

            base.Process(context, output);
        }
    }
}
