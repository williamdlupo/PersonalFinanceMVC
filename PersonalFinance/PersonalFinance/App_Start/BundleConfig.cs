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
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                    "~/Content/site.css"));
            bundles.Add(new StyleBundle("~/Content/font-awesome").Include(
                    "~/Content/font-awesome.css"));
            bundles.Add(new StyleBundle("~/Content/animate").Include(
                    "~/Content/animate.css"));
            bundles.Add(new StyleBundle("~/Content/bootstrap").Include(
                    "~/Content/bootstrap.css"));
            bundles.Add(new StyleBundle("~/Content/pe-icon-7-stroke").Include(
                    "~/Content/pe-icon-7-stroke.css"));
            bundles.Add(new StyleBundle("~/Content/helper").Include(
                    "~/Content/helper.css"));
            bundles.Add(new StyleBundle("~/Content/stroke-icons-style").Include(
                    "~/Content/stroke-icons-style.css"));
            bundles.Add(new StyleBundle("~/Content/pe-icons").Include(
        "~/Content/pe-icon-7-stroke.css"));
            bundles.Add(new StyleBundle("~/Content/styles/style").Include(
                    "~/Content/style.css"));
            bundles.Add(new StyleBundle("~/Content/dataTables").Include(
                "~/Content/dataTables.min.css"));
            bundles.Add(new StyleBundle("~/Content/daterangepicker").Include(
                "~/Content/daterangepicker.css"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                    "~/Scripts/bootstrap.js"));
            bundles.Add(new ScriptBundle("~/bundles/respond").Include(
                    "~/Scripts/respond.js"));
            bundles.Add(new ScriptBundle("~/bundles/sparkline").Include(
                    "~/Scripts/index.js"));
            bundles.Add(new ScriptBundle("~/bundles/Chart").Include(
                    "~/Scripts/Chart.min.js"));
            bundles.Add(new ScriptBundle("~/bundles/luna").Include(
                    "~/Scripts/luna.js"));
            bundles.Add(new ScriptBundle("~/bundles/daterangepicker").Include(
                    "~/Scripts/daterangepicker.js"));
            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                    "~/Scripts/datatables.min.js"));
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                    "~/bundles/jqueryval"));
            bundles.Add(new ScriptBundle("~/bundles/Moment").Include(
                "~/Scripts/Moment.js"));

        }
    }
}
