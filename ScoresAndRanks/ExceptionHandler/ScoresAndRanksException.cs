namespace ScoresAndRanks.ExceptionHandler
{
    [Serializable]
    public class ScoresAndRanksException : Exception
    {
        public enum ScoresAndRanksExceptionType
        {
            SCORE_OUT_OF_RANGE,
            END_LESS_THAN_START,
            INDEX_NODE_NOT_SUPPORT
        }
        public ScoresAndRanksException() { }

        public ScoresAndRanksException(string message) : base(message) { }

        public ScoresAndRanksException(string message, Exception innerException) : base(message, innerException) { }

        public ScoresAndRanksException(ScoresAndRanksExceptionType type) : base(GetMessageFromType(type)){ }

        public ScoresAndRanksException(ScoresAndRanksExceptionType type, Exception innerException) : base (GetMessageFromType(type), innerException) { }


        private static string GetMessageFromType(ScoresAndRanksExceptionType type)
        {
            string message;
            switch (type)
            {
                case ScoresAndRanksExceptionType.SCORE_OUT_OF_RANGE:
                    message = "Score is out of range.";
                    break;
                case ScoresAndRanksExceptionType.END_LESS_THAN_START:
                    message = "'End' is less than 'Start'.";
                    break;
                case ScoresAndRanksExceptionType.INDEX_NODE_NOT_SUPPORT:
                    message = "Index node is not supported.";
                    break;
                default: message = string.Empty; break;
            }
            return message;
        }

    }
}
