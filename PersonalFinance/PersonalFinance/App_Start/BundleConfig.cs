using System.Web;
using System.Web.Optimization;

namespace PersonalFinance
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/jquery.validate*"
                        ));

            bundles.Add(new StyleBundle("~/Content/css_all").Include(
                   "~/Content/Site.css",
                   "~/Content/font-awesome.css",
                   "~/Content/font-awesome.css",
                   "~/Content/animate.css",
                   "~/Content/bootstrap.css",
                   "~/Content/helper.css",
                   "~/Content/stroke-icons/stroke-icons-style.css",
                   "~/Content/pe-icon-7-stroke.css",
                   "~/Content/style.css",
                   "~/Content/datatables.min.css",
                    "~/Content/daterangepicker.css"
       ));

            bundles.Add(new ScriptBundle("~/bundles/scripts_all").Include(
                    "~/Scripts/bootstrap.js",
                    "~/Scripts/respond.js",
                    "~/Scripts/index.js",
                    "~/Scripts/Chart.min.js",
                    "~/Scripts/luna.js",
                    "~/Scripts/datatables.min.js",
                     "~/Scripts/Moment.js",
                    "~/Scripts/daterangepicker.js"
                    ));

            BundleTable.EnableOptimizations = true;

        }
    }
}
