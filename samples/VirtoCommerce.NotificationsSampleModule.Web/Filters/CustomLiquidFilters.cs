using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scriban;

namespace VirtoCommerce.NotificationsSampleModule.Web.Filters
{
    public static class CustomLiquidFilters
    {
        public static string CustomFilter(TemplateContext context, string input)
        {
            return "my cool CustomFilter";
        }
    }
}
