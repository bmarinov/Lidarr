﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(29)]
    public class add_formats_to_naming_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NamingConfig").AddColumn("StandardEpisodeFormat").AsString().Nullable();
            Alter.Table("NamingConfig").AddColumn("DailyEpisodeFormat").AsString().Nullable();

            Execute.WithConnection(ConvertConfig);
        }

        private void ConvertConfig(IDbConnection conn, IDbTransaction tran)
        {
            using (IDbCommand namingConfigCmd = conn.CreateCommand())
            {
                namingConfigCmd.Transaction = tran;
                namingConfigCmd.CommandText = @"SELECT * FROM NamingConfig LIMIT 1";
                using (IDataReader namingConfigReader = namingConfigCmd.ExecuteReader())
                {
                    while (namingConfigReader.Read())
                    {
                        var separator = namingConfigReader.GetString(1);
                        var numberStyle = namingConfigReader.GetInt32(2);
                        var includeSeriesTitle = namingConfigReader.GetBoolean(3);
                        var includeEpisodeTitle = namingConfigReader.GetBoolean(5);
                        var includeQuality = namingConfigReader.GetBoolean(6);
                        var replaceSpaces = namingConfigReader.GetBoolean(7);

                        //Output settings
                        var seriesTitlePattern = "";
                        var episodeTitlePattern = "";
                        var dailyEpisodePattern = "{Air Date}";
                        var qualityFormat = " [{Quality Title}]";

                        if (includeSeriesTitle)
                        {
                            seriesTitlePattern = "{Series Title}" + separator;

                            if (replaceSpaces)
                            {
                                seriesTitlePattern = "{Series.Title}" + separator;
                            }
                        }

                        if (includeEpisodeTitle)
                        {
                            episodeTitlePattern = separator + "{Episode Title}";

                            if (replaceSpaces)
                            {
                                episodeTitlePattern = separator + "{Episode.Title}";
                            }
                        }

                        if (replaceSpaces)
                        {
                            dailyEpisodePattern = "{Air.Date}";
                        }

                        var standardEpisodeFormat = String.Format("{0}{1}{2}", seriesTitlePattern,
                                                                             GetNumberStyle(numberStyle).Pattern,
                                                                             episodeTitlePattern);

                        var dailyEpisodeFormat = String.Format("{0}{1}{2}", seriesTitlePattern,
                                                                            dailyEpisodePattern,
                                                                            episodeTitlePattern);

                        if (includeQuality)
                        {
                            if (replaceSpaces)
                            {
                                qualityFormat = " [{Quality.Title}]";
                            }

                            standardEpisodeFormat += qualityFormat;
                            dailyEpisodeFormat += qualityFormat;
                        }

                        using (IDbCommand updateCmd = conn.CreateCommand())
                        {
                            var text = String.Format("UPDATE NamingConfig " +
                                                     "SET StandardEpisodeFormat = '{0}', " +
                                                     "DailyEpisodeFormat = '{1}'",
                                                     standardEpisodeFormat,
                                                     dailyEpisodeFormat);

                            updateCmd.Transaction = tran;
                            updateCmd.CommandText = text;
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private static readonly List<dynamic> NumberStyles = new List<dynamic>
                                                                            {
                                                                                new
                                                                                    {
                                                                                        Id = 0,
                                                                                        Name = "1x05",
                                                                                        Pattern = "{season}x{0episode}",
                                                                                        EpisodeSeparator = "x"

                                                                                    },
                                                                                new 
                                                                                    {
                                                                                        Id = 1,
                                                                                        Name = "01x05",
                                                                                        Pattern = "{0season}x{0episode}",
                                                                                        EpisodeSeparator = "x"
                                                                                    },
                                                                                new
                                                                                    {
                                                                                        Id = 2,
                                                                                        Name = "S01E05",
                                                                                        Pattern = "S{0season}E{0episode}",
                                                                                        EpisodeSeparator = "E"
                                                                                    },
                                                                                new
                                                                                    {
                                                                                        Id = 3,
                                                                                        Name = "s01e05",
                                                                                        Pattern = "s{0season}e{0episode}",
                                                                                        EpisodeSeparator = "e"
                                                                                    }
                                                                            };

        private static dynamic GetNumberStyle(int id)
        {
            return NumberStyles.Single(s => s.Id == id);
        }
    }
}
