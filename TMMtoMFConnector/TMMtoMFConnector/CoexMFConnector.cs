using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using MFilesAPI.Extensions;
using MFilesAPI;


namespace TMMtoMFConnector
{
    public class CoexMFConnector
    {        
        private string serverName;
        private string port;
        private string protocol;
        private string vaultGUID;
        private string user;
        private string pw;
        private int uID;
        private string uGUID;
        private SessionInfo session = null;
        private byte [] sessionID = null;

        private MFilesServerApplication mfserver;
        private Vault vault;
        // preconfigured root viewIDs 
        private static List<int> rootFolderDefIds;
        /// <summary>
        /// List containing information on all elements in the current folder.
        /// </summary>
        public static List<FolderElement> currentFolderContent;
        /// <summary>
        /// Boolean to determine if backwards navigation is possible. Disable back-button if this is false.
        /// </summary>
        public static Boolean canNavigateBackwards = false;
        // folderdefs collection to keep track of where in the view hierarchy navigation currently is.        
        private static FolderDefs currentFolderDefs = new FolderDefs();       
        // FildownLoadLocation where files from M-Files will be temporarily stored        
        private static FileDownloadLocation loc;
        /// <summary>
        /// Initializes component. Loads config, connects to M-Files server and logs in to Vault.
        /// </summary>
        public void Initialize()
        {
            this.LoadAppSettiings();
            this.ReadCustomSection();
            this.Login();
            this.PopulateFileDownloadLocation();
        }
        /// <summary>
        /// Method to retrieve all views defined as the root views from config. All these should be predefined and on one level i.e not one within another.
        /// </summary>
        /// <returns>The List of FolderElement objects representing the pre-configured views.</returns>
        public List<FolderElement> GetRootDocumentViews()
        {
            // Create FolderElement for each of the configured root views
            currentFolderContent = new List<FolderElement>();
            foreach (var item in rootFolderDefIds)
            {
                var view = vault.ViewOperations.GetView(item);
                var elm = new FolderElement()
                {
                    ElementId = item,
                    ElementName = view.Name,
                    ElementType = ElementType.ViewFolder
                };
                
                currentFolderContent.Add(elm);                
            }
            ListCurrentFolderElements(currentFolderContent); // for testing!!
            return currentFolderContent;
            
        }
        /// <summary>
        /// Open desired FolderElement and retrieves its content.
        /// Functionality for the M-Files FolderContentItem types: traditonal folder and external folder has not been implemented.
        /// </summary>
        /// <param name="elm">The FolderElement clicked in the user interface and passed to this function. If the FolderElement passed does not contain a valid lookup id,
        /// the ElementName property is used for resolving the next level.</param>
        /// <returns>List of FolderElements that represent the contents of the clicked FolderElement.</returns>
        /// <throws>Exception if a FolderContentItem is of either traditional or external type</throws>
        public List<FolderElement> GetNextFolder (FolderElement elm)
        {        
            // Sanity!!
            if (null == elm)
            {
                throw new ArgumentNullException();
            }
            FolderDef fDef = new FolderDef();
            TypedValue tv = new TypedValue();
            if (elm.ElementType == ElementType.ViewFolder)
            {
                if (0 != elm.ElementId)
                    fDef.SetView(elm.ElementId);
                else
                    throw new Exception(String.Format("A folderelement of type {0} was passed, containing an invalid elementId for the ViewFolder.", elm.ElementType.ToString()));
            }
            if (elm.ElementType == ElementType.PropertyFolderVL)
            {
                if (0 != elm.ElementId)
                {                    
                    tv.SetValue(MFDataType.MFDatatypeLookup, elm.ElementId);
                    fDef.SetPropertyFolder(tv);
                }
                else
                    throw new Exception(String.Format("A folderelement of type {0} was passed, containing an invalid elementId for the PropertyFolder.", elm.ElementType.ToString()));
            }
            if (elm.ElementType == ElementType.PropertyFolderText)
            {
                if (elm.ElementName != "")
                {
                    tv.SetValue(MFDataType.MFDatatypeText, elm.ElementName);
                    fDef.SetPropertyFolder(tv); 
                }
                else
                    throw new Exception(String.Format("A folderelement of type {0} was passed, containing an empty ElementName property.", elm.ElementType.ToString()));
            }
            if (elm.ElementType == ElementType.PropertyFolderInt)
            {
                if (elm.ElementName != "" && int.TryParse(elm.ElementName, out int res))
                {
                    tv.SetValue(MFDataType.MFDatatypeInteger, res);
                    fDef.SetPropertyFolder(tv); 
                }
                else
                    throw new Exception(String.Format("A folderelement of type {0} was passed. Unable to parse the ElementName property: {1}, it is either not a number or an empty string.", elm.ElementType.ToString(), elm.ElementName));
            }
            if (elm.ElementType == ElementType.PropertyFolderDbl)
            {
                if (elm.ElementName != "" && decimal.TryParse(elm.ElementName, out decimal res))
                {
                    tv.SetValue(MFDataType.MFDatatypeFloating, res);
                    fDef.SetPropertyFolder(tv); 
                }
                else
                    throw new Exception(String.Format("A folderelement of type {0} was passed. Unable to parse the ElementName property: {1}, it is either not a decimal or an empty string.", elm.ElementType.ToString(), elm.ElementName));
            }            
            currentFolderDefs.Add(-1, fDef);
            return GetElements(currentFolderDefs, elm);
            
        }
        /// <summary>
        /// Gets the contents of the previous level in the view hierarchy.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns>List<FolderElement> collection containing the previous levels content</returns>
        public List<FolderElement> GetPreviousLevel()
        {
            // sanity
            if (canNavigateBackwards)
            {
                if (currentFolderDefs.Count == 1)
                {
                    currentFolderDefs.Remove(currentFolderDefs.Count);
                    canNavigateBackwards = false;
                    return GetRootDocumentViews();
                }
                if (currentFolderDefs.Count > 1)
                {
                    currentFolderDefs.Remove(currentFolderDefs.Count);
                    return GetElements(currentFolderDefs);
                }
            }
            return GetRootDocumentViews();
        }
        /// <summary>
        /// Downloads the files of the desired FolderElement to a temporary location.
        /// </summary>
        /// <param name="elm">The FolderElement clicked in the user interface and passed to this function. FolderElement.ElementType has to be ElementType.Document.</param>
        /// <returns>String representing path to temporary file location.</returns>
        public String GetElementFiles (FolderElement elm)
        {
            //at this point the elm should have been populated with an objectId and type should be document
            if(elm.ElementType == ElementType.Document || elm.ElementType == ElementType.MultiFile)
            {
                //single- or multi-file document download the file and return the path
                var oId = new ObjID();
                oId.SetIDs(0, elm.ElementId);
                var objVer = vault.ObjectOperations.GetLatestObjVerEx(oId, true);
                var objFiles = vault.ObjectFileOperations.GetFiles(objVer);
                if(objFiles.Count == 1)
                {
                    //only one file
                    var filedl = loc.DownloadFile(objFiles[1], vault);
                    System.Diagnostics.Process.Start(filedl.TargetFile.FullName);
                    return filedl.TargetFile.FullName;
                }
                if(objFiles.Count > 1)
                {
                    //several files
                    var dirName = "";
                    foreach(ObjectFile f in objFiles)
                    {
                        var filedl = loc.DownloadFile(f, vault);
                        System.Diagnostics.Process.Start(filedl.TargetFile.FullName); 
                        if (dirName == "")
                        {
                            dirName = filedl.TargetFile.DirectoryName;
                        }
                    }
                    return dirName;
                }
                else
                {
                    throw new ArgumentNullException(elm.ElementName, String.Format("The FolderElement {0} does not contain any files", elm.ElementName));
                }
            }
            else
            {
                throw new FolderElementTypeException(elm.ElementType.ToString());
            }
        }
        /// <summary>
        /// Deletes downloaded files. Call this when the files are no longer in use.
        /// </summary>
        public void DeleteTempFiles()
        {
            loc.CleanTemporaryFiles();
        }
        /// <summary>
        /// Clears the components data and logs out of the M-Files server.
        /// </summary>
        public void UnloadComponent()
        {
            //TODO!!
            if (sessionID != null)
            {
                var msg = vault.DetachSession();
                Console.WriteLine("Detaching session from vault: " + msg);
                mfserver.Disconnect();
                vault = null;
                mfserver = null;
            }
            else
            {
                sessionID = Encoding.Unicode.GetBytes(vault.LoginSessionID);
                vault.LogOutSilent();
                mfserver.Disconnect();
                vault = null;
                mfserver = null;
            }
            currentFolderContent = null;
        }
        // Helper method to get FolderContentItems from the vault and convert them to FolderElements
        private List<FolderElement> GetElements(FolderDefs folderDefs, FolderElement elm = null)
        {
            currentFolderContent = new List<FolderElement>();
            // get the collection of FolderContentItems
            var content = vault.ViewOperations.GetFolderContents(folderDefs);
            if (content.Count == 0)
            {
                // the selscted FolderElement is an empty folder. Can be prevented by setting filter on View to only show documents.
                FolderElement e = new FolderElement() { ElementId = 0, ElementName = "Nothing to see here!", ElementType = ElementType.PropertyFolderVL };
                currentFolderContent.Add(e);
                ListCurrentFolderElements(currentFolderContent); // for testing!!
                return currentFolderContent;
                //Optionally we throw an exception
                throw new EmptyFolderException(folderDefs.Count, elm.ElementName);
            }
            foreach (var item in content)
            {
                FolderElement e = new FolderElement();
                if ((item as FolderContentItem).FolderContentItemType == MFFolderContentItemType.MFFolderContentItemTypePropertyFolder)
                {
                    //convert to FolderElement with appropriate data and add to list of folderelement
                    TypedValue tv = (item as FolderContentItem).PropertyFolder;
                    e.ElementName = tv.DisplayValue;
                    if (tv.DataType == MFDataType.MFDatatypeLookup)
                    {
                        e.ElementId = tv.GetLookupID();
                        e.ElementType = ElementType.PropertyFolderVL;
                    }
                    else
                    {
                        e.ElementId = 0;
                    }
                    if (tv.DataType == MFDataType.MFDatatypeText)
                    {
                        e.ElementType = ElementType.PropertyFolderText;
                    }
                    if (tv.DataType == MFDataType.MFDatatypeInteger)
                    {
                        e.ElementType = ElementType.PropertyFolderInt;
                    }
                    if (tv.DataType == MFDataType.MFDatatypeFloating)
                    {
                        e.ElementType = ElementType.PropertyFolderDbl;
                    }
                    currentFolderContent.Add(e);
                    continue;
                }

                if ((item as FolderContentItem).FolderContentItemType == MFFolderContentItemType.MFFolderContentItemTypeViewFolder)
                {
                    //convert to FolderElement with appropriate data and add to list of folderelement
                    View v = (item as FolderContentItem).View;
                    e.ElementName = v.Name;
                    e.ElementId = v.ID;
                    e.ElementType = ElementType.ViewFolder;
                    
                    currentFolderContent.Add(e);
                    continue;
                }
                if ((item as FolderContentItem).FolderContentItemType == MFFolderContentItemType.MFFolderContentItemTypeObjectVersion)
                {
                    //convert to FolderElement with appropriate data and add to list of folderelement
                    ObjectVersion obj = (item as FolderContentItem).ObjectVersion;
                    // we will not display objects that are not documents. Depending on customers use, we may need  implement functionality that works for document collections
                    if (obj.ObjVer.Type != (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument || obj.FilesCount == 0)
                    {
                        continue;
                    }
                    e.ElementName = obj.Title;
                    e.ElementId = obj.ObjVer.ID;
                    if (obj.FilesCount != 1 && obj.FilesCount > 0)
                    {
                        e.ElementType = ElementType.MultiFile;
                    }
                    else
                    {
                        e.ElementType = ElementType.Document;
                    }
                    
                    currentFolderContent.Add(e);
                    continue;
                }
                else
                    throw new NotImplementedException(String.Format("Unable to process FolderContentItem of type: {0}", (item as FolderContentItem).FolderContentItemType.ToString()));


                //convert to FolderElement with appropriate data and add to list of folderelement

            }
            if(!(currentFolderContent.Count > 0))
            {
                //the selected folder contains only empty multi-file documents
                FolderElement e = new FolderElement() { ElementId = 0, ElementName = "Nothing to see here!", ElementType = ElementType.PropertyFolderVL };
                currentFolderContent.Add(e);
                ListCurrentFolderElements(currentFolderContent); // for testing!!
                return currentFolderContent;
                //Optionally we throw an exception
                throw new EmptyFolderException(folderDefs.Count, elm.ElementName);
            }
            ListCurrentFolderElements(currentFolderContent); // for testing!!
            // Set the global boolean canNavigateBackwards to true
            canNavigateBackwards = true;
            //return list of FolderElements
            return currentFolderContent;
        }
        private void ListCurrentFolderElements(List<FolderElement> elements)
        {
            foreach (var item in elements)
            {
                Console.WriteLine("Element id: {0}, Element name: {1}, Element type: {2}.", item.ElementId, item.ElementName, item.ElementType); 
            }
        }

        private void ListFolderContentItems(FolderContentItems folderContentItems)
        {
            foreach(FolderContentItem f in folderContentItems)
            {
                switch(f.FolderContentItemType)
                {
                    case MFFolderContentItemType.MFFolderContentItemTypeViewFolder:
                        Console.WriteLine("MFFolderContentItemType.MFFolderContentItemTypeViewFolder"); // do something with View
                        Console.WriteLine("View id: {0}, View name: {1}, Viewtype: {2}, IsCommon: {3}, HasParent: {4}.", f.View.ID, f.View.Name, f.View.ViewType, f.View.Common, f.View.HasParent);
                        break;
                    case MFFolderContentItemType.MFFolderContentItemTypeUnknown:
                        Console.WriteLine("MFFolderContentItemType.MFFolderContentItemTypeUnknown"); // do something with Unknown?
                        break;
                    case MFFolderContentItemType.MFFolderContentItemTypeTraditionalFolder:
                        Console.WriteLine("MFFolderContentItemType.MFFolderContentItemTypeTraditionalFolder"); // do something with traditional folder
                        Console.WriteLine("Traditional folder DisplayValue: {0}, ObjectType: {1}, ObjectFlags: {2}.", f.TraditionalFolder.DisplayValue, f.TraditionalFolder.ObjectType, f.TraditionalFolder.ObjectFlags);
                        break;
                    case MFFolderContentItemType.MFFolderContentItemTypePropertyFolder:
                        Console.WriteLine("MFFolderContentItemType.MFFolderContentItemTypePropertyFolder"); // do something with propertyfolder (grouping)
                        Console.WriteLine("PropertyFolder DisplayValue: {0}, Value (ID means valuelist lookup): {1}.",f.PropertyFolder.DisplayValue, f.PropertyFolder.Value);
                        break;
                    case MFFolderContentItemType.MFFolderContentItemTypeObjectVersion:
                        Console.WriteLine("MFFolderContentItemType.MFFolderContentItemTypeObjectVersion"); // do something with object
                        Console.WriteLine("ObjectVersion Title: {0}, FilesCount: {1}, Class: {2}.", f.ObjectVersion.Title, f.ObjectVersion.FilesCount, f.ObjectVersion.Class);
                        break;
                    case MFFolderContentItemType.MFFolderContentItemTypeExternalViewFolder:
                        Console.WriteLine("MFFolderContentItemType.MFFolderContentItemTypeExternalViewFolder"); // do something with object
                        Console.WriteLine("ExternalView Displayname: {0}, ID: {1}.", f.ExternalView.DisplayName, f.ExternalView.ID);
                        break;
                    default:                        
                        break;

                }
            }
        }       
        private void ListCurrentDocViewsInfo(List<MFilesAPI.View> docViews)
        {
            if(docViews.Count > 0)
            {
                foreach(MFilesAPI.View v in docViews)
                {
                    Console.WriteLine("View name: " + v.Name + ", view ID : " + v.ID + ". Do you have a parent? " + v.HasParent + "M-Files URL: " + vault.ViewOperations.GetMFilesURLForView(v.ID, null, true));
                }
            }
            else
            {
                Console.WriteLine("There are no views in the collection");
            }
        }

        /// <summary>
        /// Loads configurations from App.config or sets default. User and Pw needs to be supplied in cfg.
        /// </summary>
        private void LoadAppSettiings()
        {
            var appSettings = ConfigurationManager.GetSection("ApplicationSettings")
                as NameValueCollection;
            
            if(appSettings.Count == 0)
            {
                Console.WriteLine("Application Settings are not defined!");
            }
            else
            {
                if (appSettings.Get("ServerName") == "")
                    serverName = "localhost";
                else
                    serverName = appSettings.Get("ServerName");
                if (appSettings.Get("Port") == "")
                    port = "2266";
                else
                    port = appSettings.Get("Port");
                if (appSettings.Get("Protocol") == "")
                    protocol = "ncacn_ip_tcp";
                else
                    protocol = appSettings.Get("Protocol");
                vaultGUID = appSettings.Get("VaultGUID");
                user = appSettings.Get("User");
                pw = appSettings.Get("Pw");
                uGUID = appSettings.Get("uGUID");
            }
            Console.WriteLine("Config loaded!");
        }

        /// <summary>
        /// Method for connecting to the M-Files server and logging into the Vault
        /// </summary>
        private void Login()
        {
            mfserver = new MFilesAPI.MFilesServerApplication();
            if (sessionID != null)
            {
                vault = mfserver.ConnectWithExistingVaultSession(sessionID, protocol, serverName, port, false);
                return;
            }
            var tmz = new MFilesAPI.TimeZoneInformation();
            tmz.LoadWithCurrentTimeZone();
            var res = mfserver.ConnectEx3(tmz, MFilesAPI.MFAuthType.MFAuthTypeSpecificMFilesUser, user, pw, "", "", protocol, serverName, port, false, "", false, true, "");
            if (res == MFilesAPI.MFServerConnection.MFServerConnectionAnonymous)
            {
                // need to decide what to do with error situations!
                Console.WriteLine("Invalid credentials provided");
                return;
            }
            if (res == MFilesAPI.MFServerConnection.MFServerConnectionNone)
            {
                // need to decide what to do with error situations!
                Console.WriteLine("Unable to connect to server");
                return;
            }
            else
            {
                if (session == null)
                {
                    vault = mfserver.LogInAsUserToVaultEx(vaultGUID, tmz, MFilesAPI.MFAuthType.MFAuthTypeSpecificMFilesUser, user, pw, "", "");
                    session = vault.SessionInfo;
                    sessionID = Encoding.Unicode.GetBytes(vault.LoginSessionID);
                    Console.WriteLine("connected to vault: " + vault.Name);
                    return;
                }

            }

        }
        /// <summary>
        /// Resolves userGUID to userID. UserGUID must be entered in config 
        /// </summary>
        private void GetUserID()
        {
            uID = vault.UserOperations.GetUserIDByGUID(uGUID);
        }
        private void ReadCustomSection()
        {
            rootFolderDefIds = new List<int>();
            try
            {
                // Get the application configuration file.
                //System.Configuration.Configuration config =
                //        ConfigurationManager.OpenExeConfiguration(
                //        ConfigurationUserLevel.None) as Configuration;

                // Read and display the custom section.
                ConfiguredViewsSection myViewsSection = System.Configuration.ConfigurationManager.GetSection("ConfiguredViews") as ConfiguredViewsSection;

                if (myViewsSection == null)
                {
                    Console.WriteLine("Failed to load ConfiguredViews.");
                }
                else
                {
                    Console.WriteLine("Collection elements contained in the custom section collection:");
                    for (int i = 0; i < myViewsSection.Views.Count; i++)
                    {
                        Console.WriteLine("   Id={0}",
                            myViewsSection.Views[i].Id);
                        rootFolderDefIds.Insert(i, myViewsSection.Views[i].Id);
                    }
                    
                }
            }
            catch (ConfigurationErrorsException err)
            {
                Console.WriteLine("ReadCustomSection(string): {0}", err.ToString());
            }
        }
        private void PopulateFileDownloadLocation()
        {
            loc = new FileDownloadLocation() { CleanDirectoryOnDisposal = true };
        }
    }    
}
