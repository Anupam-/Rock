﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using System.Text;

namespace RockWeb.Blocks.Cms
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Active Users" )]
    [Category( "CMS" )]
    [Description( "Displays a list of active users of a website." )]
    [SiteField("Site", "Site to show current active users for.", true)]
    [BooleanField("Show Last Pages", "Shows last pages in a tooltip.", true)]
    public partial class ActiveUsers : Rock.Web.UI.RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods
        
        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            ShowActiveUsers();
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the PageLiquid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated(object sender, EventArgs e)
        {
            ShowActiveUsers();
        }

        #endregion

        #region Methods

       // helper functional methods (like BindGrid(), etc.)
        private void ShowActiveUsers()
        {
            if ( !string.IsNullOrEmpty( GetAttributeValue( "Site" ) ) )
            {
                StringBuilder sbUsers = new StringBuilder();
                
                var site = SiteCache.Read( (int)GetAttributeValue( "Site" ).AsInteger() );
                lSiteName.Text = site.Name;
                lMessages.Text = string.Empty;

                // get active users
                UserLoginService loginService = new UserLoginService();
                var activeLogins = loginService.Queryable("Person")
                                    .Where( l => l.IsOnLine == true )
                                    .OrderByDescending(l => l.LastActivityDateTime);

                foreach ( var login in activeLogins )
                {
                    TimeSpan tsLastActivity =  RockDateTime.Now.Subtract((DateTime)login.LastActivityDateTime);
                    if ( tsLastActivity.Minutes <= 5 )
                    {
                        sbUsers.Append( String.Format( @"<li class='recent'><i class='fa-li fa fa-circle'></i> {0}</li>", login.Person.FullName ) );
                    }
                    else
                    {
                        sbUsers.Append( String.Format( @"<li class='not-recent'><i class='fa-li fa fa-circle '></i> {0}</li>", login.Person.FullName ) );
                    }
                }

                if ( sbUsers.Length > 0 )
                {
                    lUsers.Text = String.Format( @"<ul class='fa-ul'>{0}</ul>", sbUsers.ToString() );
                }
                else
                {
                    lMessages.Text = String.Format("<div class='alert alert-info'>No one is active on the {0} site.</div>", site.Name);
                }

            }
            else
            {
                lMessages.Text = "<div class='alert alert-warning'>No site is currently configured.</div>";
            }
        }

        #endregion
    }
}