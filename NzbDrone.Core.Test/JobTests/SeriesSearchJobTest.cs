﻿using System.Collections.Generic;

using Moq;
using NUnit.Framework;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Model.Notification;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Jobs;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test.JobTests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class SeriesSearchJobTest : CoreTest
    {
        [Test]
        public void SeriesSearch_success()
        {
            var seasons = new List<int> { 1, 2, 3, 4, 5 };

            var mocker = new AutoMoqer(MockBehavior.Strict);

            var notification = new ProgressNotification("Series Search");

            mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.GetSeasons(1)).Returns(seasons);

            mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.IsIgnored(It.IsAny<int>(), It.IsAny<int>())).Returns(false);

            mocker.GetMock<SeasonSearchJob>()
                .Setup(c => c.Start(notification, 1, It.IsAny<int>())).Verifiable();

            //Act
            mocker.Resolve<SeriesSearchJob>().Start(notification, 1, 0);

            //Assert
            mocker.VerifyAllMocks();
            mocker.GetMock<SeasonSearchJob>().Verify(c => c.Start(notification, 1, It.IsAny<int>()),
                                                       Times.Exactly(seasons.Count));
        }

        [Test]
        public void SeriesSearch_no_seasons()
        {
            var seasons = new List<int>();

            var mocker = new AutoMoqer(MockBehavior.Strict);

            var notification = new ProgressNotification("Series Search");

            mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.GetSeasons(1)).Returns(seasons);

            //Act
            mocker.Resolve<SeriesSearchJob>().Start(notification, 1, 0);

            //Assert
            mocker.VerifyAllMocks();
            mocker.GetMock<SeasonSearchJob>().Verify(c => c.Start(notification, 1, It.IsAny<int>()),
                                                       Times.Never());
        }

        [Test]
        public void SeriesSearch_should_not_search_for_season_0()
        {
            Mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.GetSeasons(It.IsAny<int>()))
                .Returns(new List<int> { 0, 1, 2 });

            Mocker.Resolve<SeriesSearchJob>().Start(MockNotification, 12, 0);


            Mocker.GetMock<SeasonSearchJob>()
                .Verify(c => c.Start(It.IsAny<ProgressNotification>(), It.IsAny<int>(), 0), Times.Never());
        }
    }
}