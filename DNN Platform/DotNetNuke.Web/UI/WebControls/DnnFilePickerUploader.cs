﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Services.Localization;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.UI.UserControls;
using DotNetNuke.Web.Common;

namespace DotNetNuke.Web.UI.WebControls
{
    public class DnnFilePickerUploader : UserControl, IFilePickerUploader
	{
		#region Private Fields
        
        private const string MyFileName = "filepickeruploader.ascx";
	    private int? _portalId = null;

        private string _fileFilter;
        private string _folderPath = String.Empty;
        private bool _folderPathSet = false;

		#endregion

		#region Protected Properties

        protected DnnFileDropDownList FilesComboBox;
        protected DnnFolderDropDownList FoldersComboBox;
        protected Label FoldersLabel;
        protected DnnFileUpload FileUploadControl;

        protected string FolderLabel
        {
            get
            {
                return Localization.GetString("Folder", Localization.GetResourceFile(this, MyFileName));
            }
        }

        protected string FileLabel
        {
            get
            {
                return Localization.GetString("File", Localization.GetResourceFile(this, MyFileName));
            }
        }

        protected string UploadFileLabel
        {
            get
            {
                return Localization.GetString("UploadFile", Localization.GetResourceFile(this, MyFileName));
            }
        }

        protected string DropFileLabel
        {
            get
            {
                return Localization.GetString("DropFile", Localization.GetResourceFile(this, MyFileName));
            }
        }
        
		#endregion

		#region Public Properties

		public bool UsePersonalFolder { get; set; }
        
        public string FilePath
        {
            get 
            {
                EnsureChildControls();

                var path = string.Empty;
                if (FoldersComboBox.SelectedFolder != null && FilesComboBox.SelectedFile != null)
                {
                    path = FilesComboBox.SelectedFile.RelativePath;
                }

                return path;
            }

            set
            {
                EnsureChildControls();
                if (!string.IsNullOrEmpty(value))
                {
                    var file = FileManager.Instance.GetFile(PortalId, value);
                    if (file != null)
                    {
                        FoldersComboBox.SelectedFolder = FolderManager.Instance.GetFolder(file.FolderId);
                        FilesComboBox.SelectedFile = file;
                    }
                }
                else
                {
                    FoldersComboBox.SelectedFolder = null;
                    FilesComboBox.SelectedFile = null;

                    LoadFolders();
                }
            }
        }
        
        public int FileID
        {
            get
            {
                EnsureChildControls();
                
                return FilesComboBox.SelectedFile != null ? FilesComboBox.SelectedFile.FileId : Null.NullInteger;
            }

            set
            {
                EnsureChildControls();
                var file = FileManager.Instance.GetFile(value);
                if (file != null)
                {
                    FoldersComboBox.SelectedFolder = FolderManager.Instance.GetFolder(file.FolderId);
                    FilesComboBox.SelectedFile = file;
                }
            }
        }

        public string FolderPath 
        { 
            get 
            {
                return _folderPathSet
                            ? _folderPath 
                            : FoldersComboBox.SelectedFolder != null 
                                ? FoldersComboBox.SelectedFolder.FolderPath 
                                : string.Empty; 
            }
            set 
            {
                _folderPath = value;
                _folderPathSet = true;
            }
        }

        public string FileFilter
        {
            get
            {
                return _fileFilter;
            }
            set
            {
                _fileFilter = value;
                if (!string.IsNullOrEmpty(value))
                {
                    FileUploadControl.Options.Extensions = value.Split(',').ToList();
                }
                else
                {
                    FileUploadControl.Options.Extensions.RemoveAll(t => true);
                }
            }
        }
        
        public bool Required { get; set; }
        
        public UserInfo User { get; set; }

	    public int PortalId
	    {
		    get
		    {
			    return !_portalId.HasValue ? PortalSettings.Current.PortalId : _portalId.Value;
		    }
			set
			{
				_portalId = value;
			}
	    }

        public bool SupportHost
        {
            get { return FileUploadControl.SupportHost; }
            set { FileUploadControl.SupportHost = value; }
        }

        #endregion

        #region Event Handlers

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            FoldersComboBox.SelectItemDefaultText = (SupportHost && PortalSettings.Current.ActiveTab.IsSuperTab) ? DynamicSharedConstants.HostRootFolder : DynamicSharedConstants.RootFolder;
            FoldersComboBox.OnClientSelectionChanged.Add("dnn.dnnFileUpload.Folders_Changed");
            FoldersComboBox.Options.Services.Parameters.Add("permission", "READ,ADD");

            FilesComboBox.OnClientSelectionChanged.Add("dnn.dnnFileUpload.Files_Changed");
            FilesComboBox.SelectItemDefaultText = DynamicSharedConstants.Unspecified;
            FilesComboBox.IncludeNoneSpecificItem = true;
            FilesComboBox.Filter = FileFilter;

            if (UrlUtils.InPopUp())
            {
                FileUploadControl.Width = 630;
                FileUploadControl.Height = 400;
            }

            LoadFolders();
            jQuery.RegisterFileUpload(Page);
            JavaScript.RequestRegistration(CommonJs.DnnPlugins);
            ServicesFramework.Instance.RequestAjaxAntiForgerySupport();
        }

        protected override void OnPreRender(EventArgs e)
        {
            if (FoldersComboBox.SelectedFolder != null && FoldersComboBox.SelectedFolder.FolderPath.StartsWith("Users/", StringComparison.InvariantCultureIgnoreCase))
            {
                var userFolder = FolderManager.Instance.GetUserFolder(User ?? UserController.Instance.GetCurrentUserInfo());
                if (FoldersComboBox.SelectedFolder.FolderID == userFolder.FolderID)
                {
                    FoldersComboBox.SelectedItem = new ListItem
                                                   {
                                                       Text = FolderManager.Instance.MyFolderName, 
                                                       Value = userFolder.FolderID.ToString(CultureInfo.InvariantCulture)
                                                   };
                }
                else if (UsePersonalFolder) //if UserPersonalFolder is true, make sure the file is under the user folder.
                {
                    FoldersComboBox.SelectedItem = new ListItem
                                                    {
                                                        Text = FolderManager.Instance.MyFolderName,
                                                        Value = userFolder.FolderID.ToString(CultureInfo.InvariantCulture)
                                                    };

                    FilesComboBox.SelectedFile = null;
                }
            }

            FoldersLabel.Text = FolderManager.Instance.MyFolderName;

            FileUploadControl.Options.FolderPicker.Disabled = UsePersonalFolder;
            if (FileUploadControl.Options.FolderPicker.Disabled && FoldersComboBox.SelectedFolder != null)
            {
                var selectedItem = new SerializableKeyValuePair<string, string>(
                    FoldersComboBox.SelectedItem.Value, FoldersComboBox.SelectedItem.Text);

                FileUploadControl.Options.FolderPicker.InitialState = new DnnDropDownListState
                                                                          {
                                                                              SelectedItem = selectedItem
                                                                                  
                                                                          };
                FileUploadControl.Options.FolderPath = FoldersComboBox.SelectedFolder.FolderPath;
            }

            base.OnPreRender(e);
        }

        #endregion

        #region Private Methods

        private void LoadFolders()
        {
            if (UsePersonalFolder)
            {
                var user = User ?? UserController.Instance.GetCurrentUserInfo();
                var userFolder = FolderManager.Instance.GetUserFolder(user);
                FoldersComboBox.SelectedFolder = userFolder;
            }
            else
            {
                //select folder
                string fileName;
                string folderPath;
                if (!string.IsNullOrEmpty(FilePath))
                {
                    fileName = FilePath.Substring(FilePath.LastIndexOf("/") + 1);
                    folderPath = string.IsNullOrEmpty(fileName) ? FilePath : FilePath.Replace(fileName, "");
                }
                else
                {
                    fileName = FilePath;
                    folderPath = FolderPath;
                }

                FoldersComboBox.SelectedFolder = FolderManager.Instance.GetFolder(PortalId, folderPath);

                if (!string.IsNullOrEmpty(fileName))
                {
                    FilesComboBox.SelectedFile = FileManager.Instance.GetFile(FoldersComboBox.SelectedFolder, fileName);
                }
            }

            FoldersComboBox.Enabled = !UsePersonalFolder;
            FoldersLabel.Visible = UsePersonalFolder;
        }

        #endregion
    }
}
