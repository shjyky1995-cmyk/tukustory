namespace Tuku.Contracts.Recognition
{
    using System;
    using System.Collections.Generic;

    public sealed class RecognitionUsage
    {
        public long? InputUnits { get; set; }

        public long? OutputUnits { get; set; }

        public int? Pages { get; set; }

        public bool IsKnown
        {
            get { return InputUnits.HasValue || OutputUnits.HasValue || Pages.HasValue; }
        }
    }

    public sealed class RecognitionRawResponse
    {
        public string Provider { get; set; }

        public string ProviderRequestId { get; set; }

        public string RawJson { get; set; }

        public DateTime ReceivedUtc { get; set; }

        public RecognitionUsage Usage { get; set; }
    }

    public sealed class RecognitionOutcome
    {
        public RecognitionOutcomeStatus Status { get; set; }

        public RecognitionRawResponse Response { get; set; }

        public string ErrorKind { get; set; }

        public string ErrorMessage { get; set; }

        public bool TimedOut { get; set; }

        public static RecognitionOutcome Succeeded(RecognitionRawResponse response)
        {
            return new RecognitionOutcome { Status = RecognitionOutcomeStatus.Succeeded, Response = response };
        }

        public static RecognitionOutcome Failed(string errorKind, string errorMessage)
        {
            return new RecognitionOutcome
            {
                Status = RecognitionOutcomeStatus.Failed,
                ErrorKind = errorKind,
                ErrorMessage = errorMessage
            };
        }

        public static RecognitionOutcome UnknownResult(string errorKind, string errorMessage)
        {
            return new RecognitionOutcome
            {
                Status = RecognitionOutcomeStatus.Unknown,
                ErrorKind = errorKind,
                ErrorMessage = errorMessage,
                TimedOut = true
            };
        }
    }
}
