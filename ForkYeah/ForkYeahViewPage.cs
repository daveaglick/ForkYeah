﻿using FluentBootstrap;
using FluentBootstrap.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ForkYeah
{
    public class ForkYeahViewPage<T> : WebViewPage<T>
    {
        public string Title
        {
            get { return GetViewDataValue<string>("Title"); }
            set { ViewData["Title"] = value; }
        }

        public MvcBootstrapHelper<T> Bs
        {
            get { return Html.Bootstrap(); }
        }

        // This returns the default value if the key is not present
        protected TType GetViewDataValue<TType>(string key)
        {
            object value;
            if (ViewData.TryGetValue(key, out value) && typeof(TType).IsAssignableFrom(value.GetType()))
            {
                return (TType)value;
            }
            return default(TType);
        }

        public override void Execute()
        {
        }
    }
}