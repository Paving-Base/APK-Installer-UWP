using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeroconf.DNS
{
    internal sealed class Response
    {
        /// <summary>
        /// List of Question records
        /// </summary>
        public List<Question> Questions;
        /// <summary>
        /// List of AnswerRR records
        /// </summary>
        public List<AnswerRR> Answers;
        /// <summary>
        /// List of AuthorityRR records
        /// </summary>
        public List<AuthorityRR> Authorities;
        /// <summary>
        /// List of AdditionalRR records
        /// </summary>
        public List<AdditionalRR> Additionals;

        public Header header;

        /// <summary>
        /// Error message, empty when no error
        /// </summary>
        public string Error;

        /// <summary>
        /// The Size of the message
        /// </summary>
        public int MessageSize;

        /// <summary>
        /// TimeStamp when cached
        /// </summary>
        public DateTime TimeStamp;

        ///// <summary>
        ///// Server which delivered this response
        ///// </summary>
        //public IPEndPoint Server;

        public Response()
        {
            Questions = [];
            Answers = [];
            Authorities = [];
            Additionals = [];

            //Server = new IPEndPoint(0, 0);
            Error = "";
            MessageSize = 0;
            TimeStamp = DateTime.Now;
            header = new Header();
        }

        public Response(/*IPEndPoint iPEndPoint,*/ byte[] data)
        {
            Error = "";
            //Server = iPEndPoint;
            TimeStamp = DateTime.Now;
            MessageSize = data.Length;
            RecordReader rr = new(data);

            Questions = [];
            Answers = [];
            Authorities = [];
            Additionals = [];

            header = new Header(rr);

            //if (header.RCODE != RCode.NoError)
            //    Error = header.RCODE.ToString();

            for (int intI = 0; intI < header.QDCOUNT; intI++)
            {
                Questions.Add(new Question(rr));
            }

            for (int intI = 0; intI < header.ANCOUNT; intI++)
            {
                Answers.Add(new AnswerRR(rr));
            }

            for (int intI = 0; intI < header.NSCOUNT; intI++)
            {
                Authorities.Add(new AuthorityRR(rr));
            }
            for (int intI = 0; intI < header.ARCOUNT; intI++)
            {
                Additionals.Add(new AdditionalRR(rr));
            }
        }

        ///// <summary>
        ///// List of RecordMX in Response.Answers
        ///// </summary>
        //public RecordMX[] RecordsMX => Answers.Select(answerRR => answerRR.RECORD).OfType<RecordMX>().ToArray();

        /// <summary>
        /// List of RecordTXT in Response.Answers
        /// </summary>
        public RecordTXT[] RecordsTXT => [.. Answers.Select(answerRR => answerRR.RECORD).OfType<RecordTXT>()];

        /// <summary>
        /// List of RecordA in Response.Answers
        /// </summary>
        public RecordA[] RecordsA => [.. Answers.Select(answerRR => answerRR.RECORD).OfType<RecordA>()];

        /// <summary>
        /// List of RecordPTR in Response.Answers
        /// </summary>
        public RecordPTR[] RecordsPTR => [.. Answers.Select(answerRR => answerRR.RECORD).OfType<RecordPTR>()];

        ///// <summary>
        ///// List of RecordCNAME in Response.Answers
        ///// </summary>
        //public RecordCNAME[] RecordsCNAME => Answers.Select(answerRR => answerRR.RECORD).OfType<RecordCNAME>().ToArray();

        /// <summary>
        /// List of RecordAAAA in Response.Answers
        /// </summary>
        public RecordAAAA[] RecordsAAAA => [.. Answers.Select(answerRR => answerRR.RECORD).OfType<RecordAAAA>()];

        ///// <summary>
        ///// List of RecordNS in Response.Answers
        ///// </summary>
        //public RecordNS[] RecordsNS => Answers.Select(answerRR => answerRR.RECORD).OfType<RecordNS>().ToArray();

        ///// <summary>
        ///// List of RecordSOA in Response.Answers
        ///// </summary>
        //public RecordSOA[] RecordsSOA => Answers.Select(answerRR => answerRR.RECORD).OfType<RecordSOA>().ToArray();

        public RR[] RecordsRR => [.. Answers, .. Authorities, .. Additionals];

        public bool IsQueryResponse => header.QR;
    }
}
