﻿using System;
using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(30)]
    public class update_series_folder_format : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(ConvertConfig);
        }

        private void ConvertConfig(IDbConnection conn, IDbTransaction tran)
        {
            using (IDbCommand namingConfigCmd = conn.CreateCommand())
            {
                namingConfigCmd.Transaction = tran;
                namingConfigCmd.CommandText = @"SELECT * FROM Config WHERE [Key] = 'SeasonFolderFormat'";
                using (IDataReader namingConfigReader = namingConfigCmd.ExecuteReader())
                {
                    while (namingConfigReader.Read())
                    {
                        var value = namingConfigReader.GetString(2);

                        value = value.Replace("%sn", "{Series Title}")
                                     .Replace("%s.n", "{Series.Title}")
                                     .Replace("%s", "{season}")
                                     .Replace("%0s", "{0season}")
                                     .Replace("%e", "{episode}")
                                     .Replace("%0e", "{0episode}");


                        using (IDbCommand updateCmd = conn.CreateCommand())
                        {
                            var text = String.Format("UPDATE Config " +
                                                     "SET [VALUE] = '{0}'" +
                                                     "WHERE [Key] = 'SeasonFolderFormat'",
                                                     value);

                            updateCmd.Transaction = tran;
                            updateCmd.CommandText = text;
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
