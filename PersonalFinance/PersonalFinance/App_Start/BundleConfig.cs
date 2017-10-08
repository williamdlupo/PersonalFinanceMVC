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
                    "~/Content/Site.css"));
            bundles.Add(new StyleBundle("~/Content/font-awesome").Include(
                    "~/Content/font-awesome.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/animate").Include(
                    "~/Content/animate.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/bootstrap").Include(
                    "~/Content/bootstrap.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/helper").Include(
                    "~/Content/helper.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/stroke-icons-style").Include(
                    "~/Content/stroke-icons/stroke-icons-style.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/icon-styling").Include(
                    "~/Content/pe-icon-7-stroke.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/styles/style").Include(
                    "~/Content/style.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/bundles/dataTables").Include(
                "~/Content/datatables.min.css", new CssRewriteUrlTransform()));
            bundles.Add(new StyleBundle("~/Content/daterangepicker").Include(
                "~/Content/daterangepicker.css", new CssRewriteUrlTransform()));

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
