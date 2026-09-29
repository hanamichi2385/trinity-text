using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resulz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.ServiceBus.MassTransit.Consumers;
using TrinityText.ServiceBus.Messages;
using TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>Publish / generate consumers against fake services (MassTransit in-memory test harness).</summary>
    [TestClass]
    [TestCategory("Offline")]
    public class PublicationConsumerTests
    {
        private sealed class State
        {
            public PublicationDTO Publication { get; set; }

            public OperationResult PublishResult { get; set; } = OperationResult.MakeSuccess();

            public OperationResult GenerateResult { get; set; } = OperationResult.MakeSuccess();

            public List<PublicationStatus> Statuses { get; } = new();

            public List<int> Removed { get; } = new();

            public int PublishCalls { get; private set; }

            public int GenerateCalls { get; private set; }

            public IPublicationService Publications => Fake.Of<IPublicationService>((m, a) =>
            {
                switch (m.Name)
                {
                    case nameof(IPublicationService.Get):
                        return Task.FromResult(OperationResult<PublicationDTO>.MakeSuccess(Publication));
                    case nameof(IPublicationService.Update):
                        Statuses.Add((PublicationStatus)a[1]);
                        return Task.FromResult(OperationResult.MakeSuccess());
                    case nameof(IPublicationService.Remove):
                        Removed.Add((int)a[0]);
                        return Task.FromResult(OperationResult.MakeSuccess());
                    default:
                        throw new NotImplementedException(m.Name);
                }
            });

            public IPublicationSupportService Support => Fake.Of<IPublicationSupportService>((m, a) =>
            {
                switch (m.Name)
                {
                    case nameof(IPublicationSupportService.Publish):
                        PublishCalls++;
                        return Task.FromResult(PublishResult);
                    case nameof(IPublicationSupportService.Generate):
                        GenerateCalls++;
                        return Task.FromResult(GenerateResult);
                    default:
                        throw new NotImplementedException(m.Name);
                }
            });
        }

        private static PublicationDTO NewPublication(PublicationStatus status = PublicationStatus.Publishing, bool withFtp = true, string email = "user@example.com")
            => new()
            {
                Id = 7,
                Website = "site",
                Email = email,
                StatusCode = status,
                FtpServer = withFtp ? new FTPServerDTO { Name = "ftp" } : null,
                ManualDelete = false,
            };

        private static async Task<ITestHarness> StartHarness(State state, Action<IBusRegistrationConfigurator> configure, ServiceProvider[] providerHolder)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(state.Publications);
            services.AddSingleton(state.Support);
            services.AddMassTransitTestHarness(configure);

            var provider = services.BuildServiceProvider(true);
            providerHolder[0] = provider;

            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();
            return harness;
        }

        [TestMethod]
        public async Task Publish_Success_RemovesThePublication_AndSendsMail()
        {
            var state = new State { Publication = NewPublication() };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<PublishWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IPublishWebsiteMessage>(new PublishWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IPublishWebsiteMessage>());
            Assert.IsTrue(await harness.Published.Any<SendMailMessage>());
            CollectionAssert.AreEqual(new[] { 7 }, state.Removed);
            Assert.AreEqual(0, state.Statuses.Count);
        }

        [TestMethod]
        public async Task Publish_Failure_KeepsThePublication_AndMarksItFailed()
        {
            var state = new State
            {
                Publication = NewPublication(),
                PublishResult = OperationResult.MakeFailure([ErrorMessage.Create("UPLOAD", "connection refused")]),
            };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<PublishWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IPublishWebsiteMessage>(new PublishWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IPublishWebsiteMessage>());
            Assert.IsTrue(await harness.Published.Any<SendMailMessage>());
            Assert.AreEqual(0, state.Removed.Count, "the row (and its ZIP) must survive a failed upload");
            CollectionAssert.AreEqual(new[] { PublicationStatus.Failed }, state.Statuses);
        }

        [TestMethod]
        public async Task Publish_WithoutEmail_DoesNotFail()
        {
            var state = new State { Publication = NewPublication(email: null) };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<PublishWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IPublishWebsiteMessage>(new PublishWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IPublishWebsiteMessage>());
            Assert.IsFalse(await harness.Published.Any<SendMailMessage>());
            Assert.IsFalse(await harness.Published.Any<Fault<IPublishWebsiteMessage>>());
            CollectionAssert.AreEqual(new[] { 7 }, state.Removed);
        }

        [TestMethod]
        public async Task Publish_AlreadyCompleted_IsSkipped()
        {
            var state = new State { Publication = NewPublication(PublicationStatus.Success) };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<PublishWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IPublishWebsiteMessage>(new PublishWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IPublishWebsiteMessage>());
            Assert.AreEqual(0, state.PublishCalls);
        }

        [TestMethod]
        public async Task Generate_AlreadyCompleted_IsNotRegenerated()
        {
            var state = new State { Publication = NewPublication(PublicationStatus.Success) };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<GenerateWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IGenerateWebsiteMessage>(new GenerateWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IGenerateWebsiteMessage>());
            Assert.AreEqual(0, state.GenerateCalls);
        }

        [TestMethod]
        public async Task Generate_PublishingPublication_OnlyResendsThePublishMessage()
        {
            var state = new State { Publication = NewPublication(PublicationStatus.Publishing) };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<GenerateWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IGenerateWebsiteMessage>(new GenerateWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IGenerateWebsiteMessage>());
            Assert.IsTrue(await harness.Published.Any<IPublishWebsiteMessage>());
            Assert.AreEqual(0, state.GenerateCalls);
        }

        [TestMethod]
        public async Task Generate_Failure_MarksFailed_AndNotifies()
        {
            var state = new State
            {
                Publication = NewPublication(PublicationStatus.Created, withFtp: false),
                GenerateResult = OperationResult.MakeFailure([ErrorMessage.Create("GENERATE", "boom")]),
            };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<GenerateWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IGenerateWebsiteMessage>(new GenerateWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IGenerateWebsiteMessage>());
            Assert.IsTrue(await harness.Published.Any<SendMailMessage>());
            CollectionAssert.AreEqual(new[] { PublicationStatus.Generating, PublicationStatus.Failed }, state.Statuses);
        }

        [TestMethod]
        public async Task Generate_Success_WithoutFtp_EndsAsSuccess()
        {
            var state = new State { Publication = NewPublication(PublicationStatus.Created, withFtp: false) };
            var holder = new ServiceProvider[1];
            var harness = await StartHarness(state, x => x.AddConsumer<GenerateWebsiteConsumer>(), holder);
            await using var _ = holder[0];

            await harness.Bus.Publish<IGenerateWebsiteMessage>(new GenerateWebsiteMessage(7, "sftp://host"));

            Assert.IsTrue(await harness.Consumed.Any<IGenerateWebsiteMessage>());
            CollectionAssert.AreEqual(new[] { PublicationStatus.Generating, PublicationStatus.Success }, state.Statuses);
            Assert.IsTrue(await harness.Published.Any<SendMailMessage>());
        }
    }
}
