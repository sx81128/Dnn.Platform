﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

#region Usings

using System;
using Microsoft.Extensions.DependencyInjection;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Mail;
using DotNetNuke.UI.Skins.Controls;
using DotNetNuke.Services.Localization;
using DotNetNuke.Abstractions;

#endregion

namespace DotNetNuke.Modules.Admin.Users
{
    /// -----------------------------------------------------------------------------
    /// Project:    DotNetNuke
    /// Namespace:  DotNetNuke.Modules.Admin.Users
    /// Class:      Membership
    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The Membership UserModuleBase is used to manage the membership aspects of a
    /// User
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class Membership : UserModuleBase
    {
        private readonly INavigationManager _navigationManager;
        public Membership()
        {
            _navigationManager = DependencyProvider.GetRequiredService<INavigationManager>();
        }

		#region "Public Properties"

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets the UserMembership associated with this control
        /// </summary>
        /// -----------------------------------------------------------------------------
        public UserMembership UserMembership
        {
            get
            {
                UserMembership membership = null;
                if (User != null)
                {
                    membership = User.Membership;
                }
                return membership;
            }
        }

		#endregion

		#region "Events"

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Raises the MembershipAuthorized Event
        /// </summary>
        /// -----------------------------------------------------------------------------


        public event EventHandler MembershipAuthorized;
        public event EventHandler MembershipPasswordUpdateChanged;
        public event EventHandler MembershipUnAuthorized;
        public event EventHandler MembershipUnLocked;
        public event EventHandler MembershipPromoteToSuperuser;
        public event EventHandler MembershipDemoteFromSuperuser;

        #endregion

		#region "Event Methods"
        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Raises the MembershipPromoteToSuperuser Event
        /// </summary>
        /// -----------------------------------------------------------------------------
        public void OnMembershipPromoteToSuperuser(EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (MembershipPromoteToSuperuser != null)
            {
                MembershipPromoteToSuperuser(this, e);
                Response.Redirect(_navigationManager.NavigateURL(), true);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Raises the MembershipPromoteToSuperuser Event
        /// </summary>
        /// -----------------------------------------------------------------------------
        public void OnMembershipDemoteFromSuperuser(EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (MembershipDemoteFromSuperuser != null)
            {
                MembershipDemoteFromSuperuser(this, e);
                Response.Redirect(_navigationManager.NavigateURL(), true);
            }
        }


        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Raises the MembershipAuthorized Event
        /// </summary>
        /// -----------------------------------------------------------------------------
        public void OnMembershipAuthorized(EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (MembershipAuthorized != null)
            {
                MembershipAuthorized(this, e);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Raises the MembershipPasswordUpdateChanged Event
        /// </summary>
        /// -----------------------------------------------------------------------------
        public void OnMembershipPasswordUpdateChanged(EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (MembershipPasswordUpdateChanged != null)
            {
                MembershipPasswordUpdateChanged(this, e);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Raises the MembershipUnAuthorized Event
        /// </summary>
        /// -----------------------------------------------------------------------------
        public void OnMembershipUnAuthorized(EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (MembershipUnAuthorized != null)
            {
                MembershipUnAuthorized(this, e);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Raises the MembershipUnLocked Event
        /// </summary>
        /// -----------------------------------------------------------------------------
        public void OnMembershipUnLocked(EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (MembershipUnLocked != null)
            {
                MembershipUnLocked(this, e);
            }
        }

		#endregion

		#region "Public Methods"

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// DataBind binds the data to the controls
        /// </summary>
        /// -----------------------------------------------------------------------------
        public override void DataBind()
        {
			//disable/enable buttons
            if (UserInfo.UserID == User.UserID)
            {
                cmdAuthorize.Visible = false;
                cmdUnAuthorize.Visible = false;
                cmdUnLock.Visible = false;
                cmdPassword.Visible = false;
            }
            else
            {
                cmdUnLock.Visible = UserMembership.LockedOut;
                cmdUnAuthorize.Visible = UserMembership.Approved && !User.IsInRole("Unverified Users");
                cmdAuthorize.Visible = !UserMembership.Approved || User.IsInRole("Unverified Users");
                cmdPassword.Visible = !UserMembership.UpdatePassword;
            }
            if (UserController.Instance.GetCurrentUserInfo().IsSuperUser && UserController.Instance.GetCurrentUserInfo().UserID!=User.UserID)
            {
                cmdToggleSuperuser.Visible = true;

                if (User.IsSuperUser)
                {
                    cmdToggleSuperuser.Text = Localization.GetString("DemoteFromSuperUser", LocalResourceFile);
                }
                else
                {
                    cmdToggleSuperuser.Text = Localization.GetString("PromoteToSuperUser", LocalResourceFile);
                }
                if (PortalController.GetPortalsByUser(User.UserID).Count == 0)
                {
                    cmdToggleSuperuser.Visible = false;
                }
            }
            lastLockoutDate.Value = UserMembership.LastLockoutDate.Year > 2000
                                        ? (object) UserMembership.LastLockoutDate
                                        : LocalizeString("Never");
            // ReSharper disable SpecifyACultureInStringConversionExplicitly
            isOnLine.Value = LocalizeString(UserMembership.IsOnLine.ToString());
            lockedOut.Value = LocalizeString(UserMembership.LockedOut.ToString());
            approved.Value = LocalizeString(UserMembership.Approved.ToString());
            updatePassword.Value = LocalizeString(UserMembership.UpdatePassword.ToString());
            isDeleted.Value = LocalizeString(UserMembership.IsDeleted.ToString());

            //show the user folder path without default parent folder, and only visible to admin.
            userFolder.Visible = UserInfo.IsInRole(PortalSettings.AdministratorRoleName);
            if (userFolder.Visible)
            {
                userFolder.Value = FolderManager.Instance.GetUserFolder(User).FolderPath.Substring(6);
            }

            // ReSharper restore SpecifyACultureInStringConversionExplicitly

            membershipForm.DataSource = UserMembership;
            membershipForm.DataBind();
        }

		#endregion

		#region "Event Handlers"

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Page_Load runs when the control is loaded
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// -----------------------------------------------------------------------------
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            cmdAuthorize.Click += cmdAuthorize_Click;
            cmdPassword.Click += cmdPassword_Click;
            cmdUnAuthorize.Click += cmdUnAuthorize_Click;
            cmdUnLock.Click += cmdUnLock_Click;
            cmdToggleSuperuser.Click+=cmdToggleSuperuser_Click;
        }


        /// -----------------------------------------------------------------------------
        /// <summary>
        /// cmdAuthorize_Click runs when the Authorize User Button is clicked
        /// </summary>
        /// -----------------------------------------------------------------------------
        private void cmdAuthorize_Click(object sender, EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (Request.IsAuthenticated != true) return;

			//Get the Membership Information from the property editors
            User.Membership = (UserMembership)membershipForm.DataSource;

            User.Membership.Approved = true;

            //Update User
            UserController.UpdateUser(PortalId, User);

            //Update User Roles if needed
            if (!User.IsSuperUser && User.IsInRole("Unverified Users") && PortalSettings.UserRegistration == (int)Common.Globals.PortalRegistrationType.VerifiedRegistration)
            {
                UserController.ApproveUser(User);
            }

            Mail.SendMail(User, MessageType.UserAuthorized, PortalSettings);

            OnMembershipAuthorized(EventArgs.Empty);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// cmdPassword_Click runs when the ChangePassword Button is clicked
        /// </summary>
        /// -----------------------------------------------------------------------------
        private void cmdPassword_Click(object sender, EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (Request.IsAuthenticated != true) return;

            bool canSend = Mail.SendMail(User, MessageType.PasswordReminder, PortalSettings) == string.Empty;
            var message = String.Empty;
            if (canSend)
            {
                //Get the Membership Information from the property editors
                User.Membership = (UserMembership)membershipForm.DataSource;

                User.Membership.UpdatePassword = true;

                //Update User
                UserController.UpdateUser(PortalId, User);

                OnMembershipPasswordUpdateChanged(EventArgs.Empty);
            }
            else
            {
                message = Localization.GetString("OptionUnavailable", LocalResourceFile);
                UI.Skins.Skin.AddModuleMessage(this, message, ModuleMessage.ModuleMessageType.YellowWarning);
            }

        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// cmdUnAuthorize_Click runs when the UnAuthorize User Button is clicked
        /// </summary>
        /// -----------------------------------------------------------------------------
        private void cmdUnAuthorize_Click(object sender, EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (Request.IsAuthenticated != true) return;

			//Get the Membership Information from the property editors
            User.Membership = (UserMembership)membershipForm.DataSource;

            User.Membership.Approved = false;

            //Update User
            UserController.UpdateUser(PortalId, User);

            OnMembershipUnAuthorized(EventArgs.Empty);
        }
        /// <summary>
        /// cmdToggleSuperuser_Click runs when the toggle superuser button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdToggleSuperuser_Click(object sender, EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (Request.IsAuthenticated != true) return;
            ////ensure only superusers can change user superuser state
            if (UserController.Instance.GetCurrentUserInfo().IsSuperUser != true) return;

            var currentSuperUserState = User.IsSuperUser;
            User.IsSuperUser = !currentSuperUserState;
            //Update User
            UserController.UpdateUser(PortalId, User);
            DataCache.ClearCache();

            if (currentSuperUserState)
            {
                OnMembershipDemoteFromSuperuser(EventArgs.Empty);
            }
            else
            {
                OnMembershipPromoteToSuperuser(EventArgs.Empty);
            }

        }
        /// -----------------------------------------------------------------------------
        /// <summary>
        /// cmdUnlock_Click runs when the Unlock Account Button is clicked
        /// </summary>
        /// -----------------------------------------------------------------------------
        private void cmdUnLock_Click(Object sender, EventArgs e)
        {
            if (IsUserOrAdmin == false)
            {
                return;
            }
            if (Request.IsAuthenticated != true) return;

			//update the user record in the database
            bool isUnLocked = UserController.UnLockUser(User);

            if (isUnLocked)
            {
                User.Membership.LockedOut = false;

                OnMembershipUnLocked(EventArgs.Empty);
            }
        }

		#endregion
    }
}
