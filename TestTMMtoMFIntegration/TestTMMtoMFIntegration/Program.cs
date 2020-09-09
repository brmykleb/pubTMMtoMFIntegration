using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMMtoMFConnector;

namespace TestTMMtoMFIntegration
{
    class Program
    {
        static void Main(string[] args)
        {
            CoexMFConnector coex = new CoexMFConnector();
            coex.Initialize();
            coex.GetRootDocumentViews();
            while (true)
            {
                Console.WriteLine("Write a letter representing the operation you want to perform:");
                Console.WriteLine("\"f\" for navigate forwards");
                Console.WriteLine("\"b\" for navigate backwards");
                Console.WriteLine("\"d\" for download/open file");
                Console.WriteLine("\"s\" to delete temporary files");
                Console.WriteLine("\"q\" to quit test");
                Console.WriteLine();
                var input = Console.ReadKey();
                switch(input.KeyChar)
                {
                    case 'f':
                        {
                            Console.WriteLine();
                            Console.WriteLine("Write the id of the element you want to navigate into:");
                            var elmId = Console.ReadLine();
                            int res;
                            FolderElement elm = null;
                            var id = int.TryParse(elmId, out res);
                            if(id && res != 0)
                            {
                                elm = CoexMFConnector.currentFolderContent.FirstOrDefault(e => e.ElementId == int.Parse(elmId));
                            }                            
                            if (null == elm)
                            {
                                elm = CoexMFConnector.currentFolderContent.FirstOrDefault(e => e.ElementName == elmId);
                            }
                            if (null == elm)
                            {
                                elm = CoexMFConnector.currentFolderContent.FirstOrDefault(e => e.ElementId == decimal.Parse(elmId));
                            }
                            var elmnts = coex.GetNextFolder(elm);
                            break;
                        }
                    case 'b':
                        {
                            var elmnts = coex.GetPreviousLevel();
                            break;
                        }
                    case 'd':
                        {
                            Console.WriteLine();
                            Console.WriteLine("Write the id of the element you want to download:");
                            var elmId = Console.ReadLine();
                            var elm = CoexMFConnector.currentFolderContent.FirstOrDefault(e => e.ElementId == int.Parse(elmId));
                            var path = coex.GetElementFiles(elm);
                            Console.WriteLine(String.Format("The path to the file is: {0}", path));
                            Console.WriteLine();
                            break;
                        }
                    case 's':
                        {
                            Console.WriteLine();
                            Console.WriteLine("Deleting temporary files!");
                            coex.DeleteTempFiles();
                            break;
                        }
                    case 'q':
                        return;
                    default:
                        break;
                }
                 
            }            
        }
    }
}
