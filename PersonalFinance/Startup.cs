using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PersonalFinance.Startup))]
namespace PersonalFinance
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
