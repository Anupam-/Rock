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
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Workflow.Action
{
    /// <summary>
    /// Sets an attribute's value to the selected value
    /// </summary>
    [Description( "Sets an attribute's value to the selected value." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Activate Activity" )]

    [WorkflowAttribute( "Attribute", "The attribute to set.", false, "", "", 0 )]
    [TextField( "Value", "The value.", false, "", "", 1 )]
    public class SetAttributeValue : CompareAction
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            Guid guid = GetAttributeValue( action, "Attribute" ).AsGuid();
            if (!guid.IsEmpty())
            {
                var attribute = AttributeCache.Read( guid );
                if ( attribute != null )
                {
                    if ( TestCompare( action ) )
                    {
                        string value = GetAttributeValue( action, "Value" );

                        if ( attribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
                        {
                            action.Activity.Workflow.SetAttributeValue( attribute.Key, value );
                            action.AddLogEntry( string.Format( "Set '{0}' attribute to '{1}'.", attribute.Name, value ) );
                        }
                        else if ( attribute.EntityTypeId == new Rock.Model.WorkflowActivity().TypeId )
                        {
                            action.Activity.SetAttributeValue( attribute.Key, value );
                            action.AddLogEntry( string.Format( "Set '{0}' attribute to '{1}'.", attribute.Name, value ) );
                        }
                    }
                }
            }


            return true;
        }

    }
}