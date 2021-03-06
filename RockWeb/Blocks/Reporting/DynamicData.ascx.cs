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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Reporting
{
    /// <summary>
    /// Block to display dynamic report, html, xml, or transformed xml based on a SQL query or stored procedure.
    /// </summary>
    [DisplayName( "Dynamic Data" )]
    [Category( "Reporting" )]
    [Description( "Block to display dynamic report, html, xml, or transformed xml based on a SQL query or stored procedure." )]

    [BooleanField( "Update Page", "If True, provides fields for updating the parent page's Name and Description", true, "", 0 )]
    [CodeEditorField( "Query", "The query to execute", CodeEditorMode.Sql, CodeEditorTheme.Rock, 400, false, "", "", 1 )]
    [TextField( "Query Params", "Parameters to pass to query", false, "", "", 2 )]
    [BooleanField( "Query is a Stored Procedure", "Is the query a stored procedure?", false, "", 3, "StoredProcedure" )]
    [TextField( "Url Mask", "The Url to redirect to when a row is clicked", false, "", "", 4 )]
    [BooleanField( "Show Columns", "Should the 'Columns' specified below be the only ones shown (vs. the only ones hidden)", false, "", 5 )]
    [TextField( "Columns", "The columns to hide or show", false, "", "", 6 )]
    [CodeEditorField( "Formatted Output", "Optional formatting to apply to the returned results.  If left blank, a grid will be displayed. Example: {% for row in rows %} {{ row.FirstName }}<br/> {% endfor %}",
        CodeEditorMode.Liquid, CodeEditorTheme.Rock, 200, false, "", "", 7 )]
    [BooleanField( "Person Report", "Is this report a list of people.?", false, "", 8 )]
    [TextField( "Merge Fields", "Any fields to make available as merge fields for any new communications", false, "", "", 9 )]
    public partial class DynamicData : RockBlock
    {
        #region Fields

        Dictionary<int, string> _sortExpressions = new Dictionary<int, string>();
        bool _updatePage = true;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += DynamicData_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            BuildControls( !Page.IsPostBack );

            _updatePage = GetAttributeValue( "UpdatePage" ).AsBoolean( true );
        }

        /// <summary>
        /// Adds icons to the configuration area of a <see cref="Rock.Model.Block" /> instance.  Can be overridden to
        /// add additional icons
        /// </summary>
        /// <param name="canConfig">A <see cref="System.Boolean" /> flag that indicates if the user can configure the <see cref="Rock.Model.Block" /> instance.
        /// This value will be <c>true</c> if the user is allowed to configure the <see cref="Rock.Model.Block" /> instance; otherwise <c>false</c>.</param>
        /// <param name="canEdit">A <see cref="System.Boolean" /> flag that indicates if the user can edit the <see cref="Rock.Model.Block" /> instance.
        /// This value will be <c>true</c> if the user is allowed to edit the <see cref="Rock.Model.Block" /> instance; otherwise <c>false</c>.</param>
        /// <returns>
        /// A <see cref="System.Collections.Generic.List{Control}" /> containing all the icon <see cref="System.Web.UI.Control">controls</see>
        /// that will be available to the user in the configuration area of the block instance.
        /// </returns>
        public override List<Control> GetAdministrateControls( bool canConfig, bool canEdit )
        {
            List<Control> configControls = new List<Control>();

            if ( IsUserAuthorized( Authorization.EDIT ) )
            {
                LinkButton lbEdit = new LinkButton();
                lbEdit.CssClass = "edit";
                lbEdit.ToolTip = "Edit Criteria";
                lbEdit.Click += lbEdit_Click;
                configControls.Add( lbEdit );
                HtmlGenericControl iEdit = new HtmlGenericControl( "i" );
                lbEdit.Controls.Add( iEdit );
                lbEdit.CausesValidation = false;
                iEdit.Attributes.Add( "class", "fa fa-pencil-square-o" );

                // will toggle the block config so they are no longer showing
                lbEdit.Attributes["onclick"] = "Rock.admin.pageAdmin.showBlockConfig()";

                ScriptManager.GetCurrent( this.Page ).RegisterAsyncPostBackControl( lbEdit );
            }

            configControls.AddRange( base.GetAdministrateControls( canConfig, canEdit ) );

            return configControls;
        }

        #endregion

        #region Events

        protected void lbEdit_Click( object sender, EventArgs e )
        {
            pnlEditModel.Visible = true;
            upnlContent.Update();
            mdEdit.Show();

            if ( _updatePage )
            {
                var pageCache = PageCache.Read( RockPage.PageId );
                tbName.Text = pageCache != null ? pageCache.PageTitle : string.Empty;
                tbDesc.Text = pageCache != null ? pageCache.Description : string.Empty;
            }

            tbName.Visible = _updatePage;
            tbDesc.Visible = _updatePage;

            ceQuery.Text = GetAttributeValue( "Query" );
            cbStoredProcedure.Checked = GetAttributeValue( "StoredProcedure" ).AsBoolean();
            tbParams.Text = GetAttributeValue( "QueryParams" );
            tbUrlMask.Text = GetAttributeValue( "UrlMask" );
            ddlHideShow.SelectedValue = GetAttributeValue( "ShowColumns" );
            tbColumns.Text = GetAttributeValue( "Columns" );
            ceFormattedOutput.Text = GetAttributeValue( "FormattedOutput" );
            cbPersonReport.Checked = GetAttributeValue( "PersonReport" ).AsBoolean();
            tbMergeFields.Text = GetAttributeValue( "MergeFields" );
        }

        void DynamicData_BlockUpdated( object sender, EventArgs e )
        {
            BuildControls( true );
        }

        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( _updatePage )
            {
                var pageCache = PageCache.Read( RockPage.PageId );
                if ( pageCache != null &&
                    ( pageCache.PageTitle != tbName.Text || pageCache.Description != tbDesc.Text ) )
                {
                    var rockContext = new RockContext();
                    var service = new PageService( rockContext );
                    var page = service.Get( pageCache.Id );
                    page.InternalName = tbName.Text;
                    page.PageTitle = tbName.Text;
                    page.BrowserTitle = tbName.Text;
                    page.Description = tbDesc.Text;
                    rockContext.SaveChanges();

                    Rock.Web.Cache.PageCache.Flush( page.Id );
                    pageCache = PageCache.Read( RockPage.PageId );

                    var breadCrumb = RockPage.BreadCrumbs.Where( c => c.Url == RockPage.PageReference.BuildUrl() ).FirstOrDefault();
                    if ( breadCrumb != null )
                    {
                        breadCrumb.Name = pageCache.BreadCrumbText;
                    }
                }
            }

            SetAttributeValue( "Query", ceQuery.Text );
            SetAttributeValue( "StoredProcedure", cbStoredProcedure.Checked.ToString() );
            SetAttributeValue( "QueryParams", tbParams.Text );
            SetAttributeValue( "UrlMask", tbUrlMask.Text );
            SetAttributeValue( "Columns", tbColumns.Text );
            SetAttributeValue( "ShowColumns", ddlHideShow.SelectedValue );
            SetAttributeValue( "FormattedOutput", ceFormattedOutput.Text );
            SetAttributeValue( "PersonReport", cbPersonReport.Checked.ToString() );
            SetAttributeValue( "MergeFields", tbMergeFields.Text );
            SaveAttributeValues();

            mdEdit.Hide();
            pnlEditModel.Visible = false;
            upnlContent.Update();

            BuildControls( true );
        }

        /// <summary>
        /// Handles the RowSelected event of the gReport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gReport_RowSelected( object sender, RowEventArgs e )
        {
            Grid grid = sender as Grid;
            string url = GetAttributeValue( "UrlMask" );
            if ( grid != null && !string.IsNullOrWhiteSpace( url ) )
            {
                foreach ( string key in grid.DataKeyNames )
                {
                    url = url.Replace( "{" + key + "}", grid.DataKeys[e.RowIndex][key].ToString() );
                }

                Response.Redirect( url, false );
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gReport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gReport_GridRebind( object sender, EventArgs e )
        {
            string errorMessage = string.Empty;
            var ds = GetData( out errorMessage );
            if ( ds != null )
            {
                int i = 0;
                foreach ( Control div in phContent.Controls )
                {
                    foreach ( var grid in div.Controls.OfType<Grid>() )
                    {
                        if ( ds.Tables.Count > i )
                        {
                            SortTable( grid, ds.Tables[i] );
                            grid.DataSource = ds.Tables[i];
                            grid.DataBind();
                            i++;
                        }
                    }
                }
            }
            upnlContent.Update();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private DataSet GetData( out string errorMessage )
        {
            errorMessage = string.Empty;

            string query = GetAttributeValue( "Query" );
            if ( !string.IsNullOrWhiteSpace( query ) )
            {
                try
                {
                    query = query.ResolveMergeFields( PageParameters() );

                    var parameters = GetParameters();
                    return DbService.GetDataSet( query, GetAttributeValue("StoredProcedure").AsBoolean(false) ? CommandType.StoredProcedure : CommandType.Text, parameters );
                }

                catch ( System.Exception ex )
                {
                    errorMessage = ex.Message;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds the controls.
        /// </summary>
        /// <param name="setData">if set to <c>true</c> [set data].</param>
        private void BuildControls( bool setData )
        {
            string errorMessage = string.Empty;
            var dataSet = GetData( out errorMessage );

            if ( !string.IsNullOrWhiteSpace( errorMessage ) )
            {
                phContent.Visible = false;

                nbError.Text = errorMessage;
                nbError.Visible = true;
            }
            else
            {
                phContent.Controls.Clear();

                if ( dataSet != null )
                {
                    string formattedOutput = GetAttributeValue( "FormattedOutput" );
                    if ( string.IsNullOrWhiteSpace( formattedOutput ) )
                    {
                        bool personReport = GetAttributeValue( "PersonReport" ).AsBoolean();

                        int tableId = 0;
                        foreach ( DataTable dataTable in dataSet.Tables )
                        {
                            var div = new HtmlGenericControl( "div" );
                            div.AddCssClass( "grid" );
                            phContent.Controls.Add( div );

                            var grid = new Grid();
                            div.Controls.Add( grid );
                            grid.ID = string.Format( "dynamic_data_{0}", tableId++ );
                            grid.AllowSorting = true;
                            grid.EmptyDataText = "No Results";
                            grid.GridRebind += gReport_GridRebind;
                            grid.RowSelected += gReport_RowSelected;
                            if ( personReport )
                            {
                                grid.PersonIdField = "Id";
                            }
                            else
                            {
                                grid.PersonIdField = null;
                            }
                            grid.CommunicateMergeFields = GetAttributeValue( "MergeFields" ).SplitDelimitedValues().ToList<string>();

                            AddGridColumns( grid, dataTable );
                            SetDataKeyNames( grid, dataTable );

                            if ( setData )
                            {
                                SortTable( grid, dataTable );
                                grid.DataSource = dataTable;
                                grid.DataBind();
                            }
                        }
                    }
                    else
                    {
                        var mergeFields = Rock.Web.Cache.GlobalAttributesCache.GetMergeFields( CurrentPerson );
                        if ( CurrentPerson != null )
                        {
                            // TODO: When support for "Person" is not supported anymore (should use "CurrentPerson" instead), remove this line
                            mergeFields.Add( "Person", CurrentPerson );
                            mergeFields.Add( "CurrentPerson", CurrentPerson );
                        }

                        mergeFields.Add( "RockVersion", Rock.VersionInfo.VersionInfo.GetRockProductVersionNumber() );
                        mergeFields.Add( "Campuses", CampusCache.All() );

                        int i = 1;
                        foreach ( DataTable dataTable in dataSet.Tables )
                        {
                            var dropRows = new List<DataRowDrop>();
                            foreach ( DataRow row in dataTable.Rows )
                            {
                                dropRows.Add( new DataRowDrop( row ) );
                            }

                            if ( dataSet.Tables.Count > 1 )
                            {
                                var tableField = new Dictionary<string, object>();
                                tableField.Add( "rows", dropRows );
                                mergeFields.Add( "table" + i.ToString(), tableField );
                            }
                            else
                            {
                                mergeFields.Add( "rows", dropRows );
                            }
                            i++;
                        }

                        phContent.Controls.Add( new LiteralControl( formattedOutput.ResolveMergeFields( mergeFields ) ) );
                    }
                }

                phContent.Visible = true;
                nbError.Visible = false;
            }

        }

        /// <summary>
        /// Sets the data key names.
        /// </summary>
        private void SetDataKeyNames(Grid grid, DataTable dataTable )
        {
            string urlMask = GetAttributeValue( "UrlMask" );
            if ( !string.IsNullOrWhiteSpace( urlMask ) )
            {
                Regex pattern = new Regex( @"\{.+\}" );
                var matches = pattern.Matches( urlMask );
                if ( matches.Count > 0 )
                {
                    string[] keyNames = new string[matches.Count];
                    for ( int i = 0; i < matches.Count; i++ )
                    {
                        keyNames[i] = matches[i].Value.TrimStart( '{' ).TrimEnd( '}' );
                    }

                    grid.DataKeyNames = keyNames;
                }
            }
            else
            {
                if (dataTable.Columns.Contains("Id"))
                {
                    grid.DataKeyNames = new string[1] { "Id" };
                }
            }
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, object> GetParameters()
        {
            string[] queryParams = GetAttributeValue( "QueryParams" ).SplitDelimitedValues();
            if ( queryParams.Length > 0 )
            {
                var parameters = new Dictionary<string, object>();
                foreach ( string queryParam in queryParams )
                {
                    string[] paramParts = queryParam.Split( '=' );
                    if ( paramParts.Length == 2 )
                    {
                        string queryParamName = paramParts[0];
                        string queryParamValue = paramParts[1];

                        // Remove leading '@' character if was included
                        if ( queryParamName.StartsWith( "@" ) )
                        {
                            queryParamName = queryParamName.Substring( 1 );
                        }

                        // If a page parameter (query or form) value matches, use it's value instead
                        string pageValue = PageParameter( queryParamName );
                        if ( !string.IsNullOrWhiteSpace( pageValue ) )
                        {
                            queryParamValue = pageValue;
                        }
                        else if ( queryParamName.ToLower() == "currentpersonid" && CurrentPerson != null )
                        {
                            // If current person id, use the value of the current person id
                            queryParamValue = CurrentPerson.Id.ToString();
                        }

                        parameters.Add( queryParamName, queryParamValue );
                    }
                }

                return parameters;
            }

            return null;
        }

        /// <summary>
        /// Adds the grid columns.
        /// </summary>
        /// <param name="dataTable">The data table.</param>
        private void AddGridColumns( Grid grid, DataTable dataTable )
        {
            bool showColumns = bool.Parse( GetAttributeValue( "ShowColumns" ) );
            var columnList = GetAttributeValue( "Columns" ).SplitDelimitedValues().ToList();

            int rowsToEval = 10;
            if ( dataTable.Rows.Count < 10 )
            {
                rowsToEval = dataTable.Rows.Count;
            }

            grid.Columns.Clear();

            if ( !string.IsNullOrWhiteSpace( grid.PersonIdField ) )
            {
                grid.Columns.Add( new SelectField() );
            }

            foreach ( DataColumn dataTableColumn in dataTable.Columns )
            {
                if ( columnList.Count > 0 &&
                    ( ( showColumns && !columnList.Contains( dataTableColumn.ColumnName, StringComparer.OrdinalIgnoreCase ) ) ||
                        ( !showColumns && columnList.Contains( dataTableColumn.ColumnName, StringComparer.OrdinalIgnoreCase ) ) ) )
                {
                    continue;
                }

                BoundField bf = new BoundField();

                if ( dataTableColumn.DataType == typeof( bool ) )
                {
                    bf = new BoolField();
                }

                else if ( dataTableColumn.DataType == typeof( DateTime ) )
                {
                    bf = new DateField();

                    for ( int i = 0; i < rowsToEval; i++ )
                    {
                        object dateObj = dataTable.Rows[i][dataTableColumn];
                        if ( dateObj is DateTime )
                        {
                            DateTime dateTime = (DateTime)dateObj;
                            if ( dateTime.TimeOfDay.Seconds != 0 )
                            {
                                bf = new DateTimeField();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    bf.HtmlEncode = false;
                }

                bf.DataField = dataTableColumn.ColumnName;
                bf.SortExpression = dataTableColumn.ColumnName;
                bf.HeaderText = dataTableColumn.ColumnName.SplitCase();
                grid.Columns.Add( bf );
            }
        }

        /// <summary>
        /// Gets the sorted view.
        /// </summary>
        /// <param name="dataTable">The data table.</param>
        /// <returns></returns>
        private void SortTable( Grid grid, DataTable dataTable)
        {
            System.Data.DataView dataView = dataTable.DefaultView;

            SortProperty sortProperty = grid.SortProperty;
            if ( sortProperty != null )
            {
                dataView.Sort = string.Format( "{0} {1}", sortProperty.Property, sortProperty.DirectionString );
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        private class DataRowDrop : DotLiquid.Drop
        {
            private readonly DataRow _dataRow;

            public DataRowDrop( DataRow dataRow )
            {
                _dataRow = dataRow;
            }

            public override object BeforeMethod( string method )
            {
                if ( _dataRow.Table.Columns.Contains( method ) )
                {
                    return _dataRow[method];
                }

                return null;
            }
        }
    }
}