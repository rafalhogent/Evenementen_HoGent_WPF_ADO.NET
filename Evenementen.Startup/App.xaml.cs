using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Evenementen.Domain;
using Evenementen.Persitence;
using Evenementen.Presentation;

using System.Windows;
using System.IO;
using System.Runtime;

namespace Evenementen.Startup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            
            IEvenementMapper evenementRepository = new EvenementMapper();
            DomainController controller = new(evenementRepository);
            EvenementenApp app = new EvenementenApp(controller);
        }


    }
}
