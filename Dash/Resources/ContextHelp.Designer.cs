﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Dash.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class ContextHelp {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ContextHelp() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Dash.Resources.ContextHelp", typeof(ContextHelp).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are required to use two factor authentication to login. If you have set up Google Authenticator before, enter the authentication code. If you have not, use the Help button to configure it..
        /// </summary>
        public static string Account_AuthCode {
            get {
                return ResourceManager.GetString("Account_AuthCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Send alerts to Microsoft Teams using a webhook connector. After creating the connector, add the URL for it here..
        /// </summary>
        public static string Alert_SendToWebhook {
            get {
                return ResourceManager.GetString("Alert_SendToWebhook", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select how you want the data for this range to be aggregated together when calculating the Y value..
        /// </summary>
        public static string ChartRange_AggregatorId {
            get {
                return ResourceManager.GetString("ChartRange_AggregatorId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select to color to use to display this range on the chart. This color will be used to show the data for this range, and a lighter version of this color will be used for the Y axis for this range..
        /// </summary>
        public static string ChartRange_Color {
            get {
                return ResourceManager.GetString("ChartRange_Color", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the size of the periods that you want to group time series data into.  If the X axis column is a date/time, X axis ticks/labels will be calculated using this period..
        /// </summary>
        public static string ChartRange_DateIntervalId {
            get {
                return ResourceManager.GetString("ChartRange_DateIntervalId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to If enabled, gaps in time between intervals will be zero filled..
        /// </summary>
        public static string ChartRange_FillDateGaps {
            get {
                return ResourceManager.GetString("ChartRange_FillDateGaps", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the report that you want to supply data for this range. After selecting a report, you&apos;ll be able to select columns from that report for the X and Y axis..
        /// </summary>
        public static string ChartRange_ReportId {
            get {
                return ResourceManager.GetString("ChartRange_ReportId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the column to use for the X axis. The X axis column values from the first range will be used as ticks/labels for the X axis. Use the up and down arrows to the right to re-order ranges and change which one is used for the X axis labels..
        /// </summary>
        public static string ChartRange_XAxisId {
            get {
                return ResourceManager.GetString("ChartRange_XAxisId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the column to use for the Y axis. The Y axis column values from the first range will be used as ticks/labels for the Y axis on the left side of the chart. Subsequent Y axis columns will be used as ticks/labels on the right side..
        /// </summary>
        public static string ChartRange_YAxisId {
            get {
                return ResourceManager.GetString("ChartRange_YAxisId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Some databases offer paging using OFFSET or LIMIT.  SQL Server 2008 R2 and below does not support this.  Check this unless you are using SQL Server 2008 R2 or lower..
        /// </summary>
        public static string Database_AllowPaging {
            get {
                return ResourceManager.GetString("Database_AllowPaging", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Re-enter the password for confirmation..
        /// </summary>
        public static string Database_ConfirmPassword {
            get {
                return ResourceManager.GetString("Database_ConfirmPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to If a connection string is provided, it will be used instead of building one from the other fields..
        /// </summary>
        public static string Database_ConnectionString {
            get {
                return ResourceManager.GetString("Database_ConnectionString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Database name is the name of the database on the server. .
        /// </summary>
        public static string Database_DatabaseName {
            get {
                return ResourceManager.GetString("Database_DatabaseName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Host is the DNS name or IP of the database server..
        /// </summary>
        public static string Database_Host {
            get {
                return ResourceManager.GetString("Database_Host", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Check this to confirm that no password is needed..
        /// </summary>
        public static string Database_IsEmptyPassword {
            get {
                return ResourceManager.GetString("Database_IsEmptyPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Name must be a unique name for this database. It does not have to be the same as the database name..
        /// </summary>
        public static string Database_Name {
            get {
                return ResourceManager.GetString("Database_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to When adding a new database, the password and matching confirm password must be entered. If editing, you can leave the password blank and it will use the existing value. Passwords are securely encrypted before being saved..
        /// </summary>
        public static string Database_Password {
            get {
                return ResourceManager.GetString("Database_Password", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Port is the port number to connect to the database server on. It will default to the default for the database type if left empty..
        /// </summary>
        public static string Database_Port {
            get {
                return ResourceManager.GetString("Database_Port", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User is the username to use when connecting to the database..
        /// </summary>
        public static string Database_User {
            get {
                return ResourceManager.GetString("Database_User", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enter a SQL WHERE statement (without the WHERE keyword) to limit the data that the user can access..
        /// </summary>
        public static string Dataset_Conditions {
            get {
                return ResourceManager.GetString("Dataset_Conditions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;Currency formats are comprised of two tokens: one representing the currency denomination and it&apos;s location in the string, and another representing the symbol for thousands, the decimal place, and the precision.&lt;/p&gt;&lt;p&gt;&lt;b&gt;{s:$}&lt;/b&gt; represents the currency denomination. The dollar sign in this example with be used for the currency denomination and can be replaced by any character.&lt;/p&gt;&lt;p&gt;&lt;b&gt;{[t:,][d:.][p:2]}&lt;/b&gt; represents the currency value. It is composed of three tokens: &lt;i&gt;[t:,]&lt;/i&gt; indicates the symbol  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string Dataset_CurrencyFormat {
            get {
                return ResourceManager.GetString("Dataset_CurrencyFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the database this dataset will query. If you need to change the database after creating the dataset, use the `Change Database` link..
        /// </summary>
        public static string Dataset_DatabaseId {
            get {
                return ResourceManager.GetString("Dataset_DatabaseId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add the roles that can access this dataset.  Administrators will be able to view/modify any dataset. .
        /// </summary>
        public static string Dataset_DatasetRole {
            get {
                return ResourceManager.GetString("Dataset_DatasetRole", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;table class=&quot;table table-hover table-bordered table-sm table-striped&quot;&gt;
        ///&lt;thead&gt;&lt;tr&gt;&lt;th&gt;Character&lt;/th&gt;&lt;th&gt;Description&lt;/th&gt;&lt;th&gt;Example&lt;/th&gt;&lt;/tr&gt;&lt;/thead&gt;
        ///&lt;tbody&gt;
        ///&lt;tr&gt;&lt;td&gt;d&lt;/td&gt;&lt;td&gt;Day of the month, 2 digits with leading zeros&lt;/td&gt;&lt;td&gt;01 to 31&lt;/td&gt;&lt;/tr&gt;
        ///&lt;tr&gt;&lt;td&gt;D&lt;/td&gt;&lt;td&gt;A textual representation of a day&lt;/td&gt;&lt;td&gt;Mon through Sun&lt;/td&gt;&lt;/tr&gt;
        ///&lt;tr&gt;&lt;td&gt;l&lt;/td&gt;&lt;td&gt;A full textual representation of the day of the week&lt;/td&gt;&lt;td&gt;Sunday through Saturday&lt;/td&gt;&lt;/tr&gt;
        ///&lt;tr&gt;&lt;td&gt;j&lt;/td&gt;&lt;td&gt;Day of the month without leading zeros&lt; [rest of string was truncated]&quot;;.
        /// </summary>
        public static string Dataset_DateFormat {
            get {
                return ResourceManager.GetString("Dataset_DateFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enter a unique name for the dataset..
        /// </summary>
        public static string Dataset_Name {
            get {
                return ResourceManager.GetString("Dataset_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the primary table or stored procedure to query for this dataset..
        /// </summary>
        public static string Dataset_PrimarySource {
            get {
                return ResourceManager.GetString("Dataset_PrimarySource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select if this dataset will query tables or a stored procedure. Using a stored proc instead of tables will disable other features like conditions and joins..
        /// </summary>
        public static string Dataset_TypeId {
            get {
                return ResourceManager.GetString("Dataset_TypeId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the table and column that should be used for this column in reports..
        /// </summary>
        public static string DatasetColumn_ColumnName {
            get {
                return ResourceManager.GetString("DatasetColumn_ColumnName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The column data type is used to create SQL for the column. Select the closest type from the list..
        /// </summary>
        public static string DatasetColumn_DataTypeId {
            get {
                return ResourceManager.GetString("DatasetColumn_DataTypeId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use this to enter SQL to modify the value returned as this column. Reference fields using SchemaName.TableName.ColumnName. This can be useful if you are using a column name that matches a keyword, like `User`..
        /// </summary>
        public static string DatasetColumn_Derived {
            get {
                return ResourceManager.GetString("DatasetColumn_Derived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use the filter query to lookup data to substitute for the value of this column, like replacing a StatusID with a name. The value for the option must be aliased as `Value` and match the value of this column. The text to display must be aliased as `Text`. Each record must have a distinct non-empty value..
        /// </summary>
        public static string DatasetColumn_FilterQuery {
            get {
                return ResourceManager.GetString("DatasetColumn_FilterQuery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The filter type determines the operators the user will have when creating report filters using this column.  The `Select` filter type lets you query another table for lookup values..
        /// </summary>
        public static string DatasetColumn_FilterTypeId {
            get {
                return ResourceManager.GetString("DatasetColumn_FilterTypeId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Check this if this is an input parameter for the procedure. Parameter columns will be visible for use when filtering a report, but will not be viewable as a column in the report..
        /// </summary>
        public static string DatasetColumn_IsParam {
            get {
                return ResourceManager.GetString("DatasetColumn_IsParam", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provide a link that clicking on the column value will open in a new window. Substitute values from columns in the row into the link using the fully qualifed column name SchemaName.TableName.ColumnName..
        /// </summary>
        public static string DatasetColumn_Link {
            get {
                return ResourceManager.GetString("DatasetColumn_Link", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The column title is a friendly column name that the user will see when creating reports..
        /// </summary>
        public static string DatasetColumn_Title {
            get {
                return ResourceManager.GetString("DatasetColumn_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Specify the type of SQL JOIN to use with this table..
        /// </summary>
        public static string DatasetJoin_JoinTypeId {
            get {
                return ResourceManager.GetString("DatasetJoin_JoinTypeId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Specify the columns and their relationship(s) that will be used when joining the tables. Use fully qualified names like SchemaName.TableName.ColumnName..
        /// </summary>
        public static string DatasetJoin_Keys {
            get {
                return ResourceManager.GetString("DatasetJoin_Keys", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enter the fully qualified table name of the table you want to join to the primary table for this dataset..
        /// </summary>
        public static string DatasetJoin_TableName {
            get {
                return ResourceManager.GetString("DatasetJoin_TableName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the column to use to filter report data..
        /// </summary>
        public static string ReportFilter_ColumnId {
            get {
                return ResourceManager.GetString("ReportFilter_ColumnId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enter a text value that you want to compare to the filter column..
        /// </summary>
        public static string ReportFilter_Criteria {
            get {
                return ResourceManager.GetString("ReportFilter_Criteria", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select the operator to use when comparing criteria..
        /// </summary>
        public static string ReportFilter_OperatorId {
            get {
                return ResourceManager.GetString("ReportFilter_OperatorId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Give this role a unique name..
        /// </summary>
        public static string Role_Name {
            get {
                return ResourceManager.GetString("Role_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This will be the default language in which this user will see the site..
        /// </summary>
        public static string User_LanguageId {
            get {
                return ResourceManager.GetString("User_LanguageId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Password must be at least 6 characters long and contain at least one special character..
        /// </summary>
        public static string User_Password {
            get {
                return ResourceManager.GetString("User_Password", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The login name for the user..
        /// </summary>
        public static string User_UserName {
            get {
                return ResourceManager.GetString("User_UserName", resourceCulture);
            }
        }
    }
}
