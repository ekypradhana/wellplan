using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ECIS.AppServer.Startup))]
namespace ECIS.AppServer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
