namespace Tuku.UnitTests.Contracts
{
    using System;
    using Tuku.Contracts.Recognition;
    using Xunit;

    public class RecognitionContractTests
    {
        [Fact]
        public void Succeeded_Outcome_CarriesRawResponse_Untrusted()
        {
            var raw = "{\"choices\":[{\"message\":{\"content\":\"楼地面：20厚水泥砂浆\"}}]}";
            var outcome = RecognitionOutcome.Succeeded(new RecognitionRawResponse
            {
                Provider = "qwen",
                ProviderRequestId = "req-9",
                RawJson = raw,
                ReceivedUtc = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc),
                Usage = new RecognitionUsage { Pages = 1 }
            });

            Assert.Equal(RecognitionOutcomeStatus.Succeeded, outcome.Status);
            Assert.Equal(raw, outcome.Response.RawJson);
            Assert.True(outcome.Response.Usage.IsKnown);
        }

        [Fact]
        public void Timeout_ProducesUnknownResult_NotSuccess()
        {
            var outcome = RecognitionOutcome.UnknownResult("timeout", "读取超时");
            Assert.Equal(RecognitionOutcomeStatus.Unknown, outcome.Status);
            Assert.True(outcome.TimedOut);
            Assert.Null(outcome.Response);
        }

        [Fact]
        public void ExtractionResult_DistinguishesNoPracticeContent_FromProblems()
        {
            var noContent = new PageExtractionResult
            {
                PageNumber = 3,
                ContentKind = PageContentKind.NoPracticeContent,
                Candidates = new System.Collections.Generic.List<ExtractedPracticeCandidate>(),
                Problems = new System.Collections.Generic.List<string>()
            };
            var needsAttention = new PageExtractionResult
            {
                PageNumber = 4,
                ContentKind = PageContentKind.NeedsAttention,
                Candidates = new System.Collections.Generic.List<ExtractedPracticeCandidate>(),
                Problems = new System.Collections.Generic.List<string> { "页面上半部模糊" }
            };

            Assert.Equal(PageContentKind.NoPracticeContent, noContent.ContentKind);
            Assert.Equal(PageContentKind.NeedsAttention, needsAttention.ContentKind);
            Assert.NotEqual(noContent.ContentKind, needsAttention.ContentKind);
        }
    }
}
