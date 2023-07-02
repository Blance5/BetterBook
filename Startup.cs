using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FinalBookProj.Startup))]
namespace FinalBookProj
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
