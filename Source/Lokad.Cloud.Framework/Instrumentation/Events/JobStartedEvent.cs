﻿#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;
using Lokad.Cloud.Jobs;

namespace Lokad.Cloud.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a job is started.
    /// </summary>
    public class JobStartedEvent : IRuntimeEvent
    {
        public EventLevel Level { get { return EventLevel.Trace; } }
        public Job Job { get; private set; }

        public JobStartedEvent(Job job)
        {
            Job = job;
        }

        public string Describe()
        {
            return string.Format("Runtime: Job {0} was started.", Job.JobId);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Framework"),
                new XElement("Event", "JobStartedEvent"),
                new XElement("Job", Job.JobId));
        }
    }
}
