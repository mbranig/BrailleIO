﻿using Gestures.InOut;
using Gestures.Recognition.GestureData;
using Gestures.Recognition.Interfaces;
using System;
using System.Collections.Generic;

namespace Gestures.Recognition
{
    /// <summary>
    /// A gesture recognizer that supports multi-touch on a touch display.
    /// </summary>
    /// <author>Dr. rer. nat. Michael Schmidt - Techniche Universität Dresden 2014.</author>
    public class GestureRecognizer : IRecognizeGestures, IRecordTemplates, ILoadGestureTemplates
    {
        #region private fields
        protected ITrackBlobs blobTracker;
        protected readonly object synchLock = new object();

        protected List<IClassify> classifiers = new List<IClassify>();
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GestureRecognizer"/> class.
        /// </summary>
        /// <param name="blobTracker">Module used for blob tracking.</param>
        public GestureRecognizer(ITrackBlobs blobTracker)
        {
            this.blobTracker = blobTracker;
        }

        #endregion

        #region public members

        #region ISynchronizable
        /// <summary>
        /// Gets the synchronization lock object.
        /// </summary>
        /// <value>The synchronization lock object.</value>
        public virtual object SynchLock { get { return synchLock; } }
        #endregion

        #region IRecognizeGestures

        private double _probabilityThreshold = 0.7;
        /// <summary>
        /// Gets or sets the probability threshold for directly accepting a classification result.
        /// </summary>
        /// <value>
        /// The probability threshold.
        /// </value>
        public double ProbabilityThreshold
        {
            get { return _probabilityThreshold; }
            set
            {
                value = Math.Min(Math.Max(0, value), 1);
                _probabilityThreshold = value;
            }
        }


        public virtual void AddClassifier(IClassify classifier)
        {
            classifiers.Add(classifier);
        }

        /// <summary>
        /// Starts the evaluation of gesture input.
        /// </summary>               
        public virtual void StartEvaluation()
        {
            if (RecordingMode) { return; }
            RecognitionMode = true;

            blobTracker.InitiateTracking();
            //blobTracker.TrackNextFrame(new Frame(frame));
        }

        public virtual String AddFrame(Frame frame)
        {
            blobTracker.AddFrame(frame);
            String result = String.Empty;
            foreach (IClassify c in classifiers)
            {
                result = c.NotifyOnInput(blobTracker.Trajectories);
            }
            return result;
        }

        public virtual IClassificationResult FinishEvaluation() { return FinishEvaluation(true); }
        public virtual IClassificationResult FinishEvaluation(bool clear)
        {
            IClassificationResult classificationResult = null;

            if (blobTracker != null)
            {
                TrackedGesture trackedGesture = blobTracker.TrackedBlobs;
                RecognitionMode = false;
                if (blobTracker.Trajectories.Count > 0)
                {
                    Console.WriteLine("Gesture trajectories to evaluate:" + blobTracker.Trajectories.Count);
                    List<IClassificationResult> oldResults = new List<IClassificationResult>();
                    // foreach (IClassify classifier in classifiers){
                    for (int i = 0; i < classifiers.Count; i++)
                    {
                        IClassify classifier = classifiers[i];
                        if (classifier != null)
                        {
                            classificationResult = classifier.Classify(blobTracker.FrameList, blobTracker.Trajectories);
                            if (classificationResult != null)
                            {
                                if (classificationResult.Probability > ProbabilityThreshold) { break; }
                                else
                                {
                                    oldResults.Add(classificationResult);
                                }
                            }
                        }
                    }

                    // no result was taken directly, so find the first one with the highest probability
                    if (oldResults != null && oldResults.Count > 0)
                    {
                        foreach (var result in oldResults)
                        {
                            if (classificationResult == null || result.Probability > classificationResult.Probability)
                                classificationResult = result;
                        }
                    }
                }
                if (clear) blobTracker.InitiateTracking();
            }
            return classificationResult;
        }

        #endregion

        #region IRecordTemplates

        public virtual void StartTemplateRecording()
        {
            if (RecognitionMode) { return; }
            RecordingMode = true;
            blobTracker.InitiateTracking();
        }

        public virtual string FinishTemplateRecording(String xmlTemplatePath, String gestureClassName)
        {
            string fileName = String.Empty;
            try
            {
                if (RecognitionMode) { return fileName; }
                RecordingMode = false;
                Gesture gesture = blobTracker.TrackedBlobs;
                GestureTemplate gestureTemplate
                    = new GestureTemplate(gestureClassName, gesture);

                foreach (Object o in classifiers)
                {
                    if (o is ILoadGestureTemplates)
                    {
                        ILoadGestureTemplates tl = (o as ILoadGestureTemplates);
                        tl.AddTemplate(gestureTemplate);
                        var classes = tl.GetGestureClasses();
                        int index = 0;
                        for (index = 0; index < classes.Length; index++)
                        {
                            if (classes[index].ClassName == gestureTemplate.ClassName)
                            {
                                break;
                            }
                        }
                        fileName = gestureTemplate.ClassName + "_" + classes[index].Count.ToString() + ".xml";
                        tl.StoreTemplate(xmlTemplatePath, fileName, gestureTemplate);
                    }
                }
            }
            catch
            {
            }
            return fileName;
        }

        #endregion

        public virtual IClassificationResult RecognizeTemplate(GestureTemplate template)
        {
            var tokenDict = new Dictionary<int, IList<Gestures.Geometrie.Vertex.Sample>>();
            foreach (var t in template.Tokens)
            {
                tokenDict.Add(t.Id, t.Samples);
            }

            foreach (IClassify o in classifiers)
            {
                var r = o.Classify(null, tokenDict);
                if (r != null) { return r; }
            }
            return null;
        }

        #region ILoadGestureTemplates Members

        static readonly StandardTemplateLoader trw = new StandardTemplateLoader();

        /// <summary>
        /// Initializes templates set with XML templates given at specified tempFileName.
        /// </summary>
        /// <param className="xmlTemplatePath">The XML gestureTemplate tempFileName.</param>
        public virtual void LoadTemplates(string xmlTemplatePath)
        {
            foreach (Object o in classifiers)
            {
                if (o is ILoadGestureTemplates)
                {
                    (o as ILoadGestureTemplates).LoadTemplates(xmlTemplatePath);
                }
            }
        }

        public virtual void AddTemplate(GestureTemplate template)
        {
            foreach (Object o in classifiers)
            {
                if (o is ILoadGestureTemplates)
                {
                    (o as ILoadGestureTemplates).AddTemplate(template);
                }
            }
        }

        public virtual void RemoveTemplates(string className)
        {
            foreach (Object o in classifiers)
            {
                if (o is ILoadGestureTemplates)
                {
                    (o as ILoadGestureTemplates).RemoveTemplates(className);
                }
            }
        }

        public virtual void StoreTemplates(string templatePath)
        {
            foreach (Object o in classifiers)
            {
                if (o is ILoadGestureTemplates)
                {
                    (o as ILoadGestureTemplates).StoreTemplates(templatePath);
                }
            }
        }

        public virtual void StoreTemplate(string templatePath, String templateFileName, GestureTemplate template)
        {
            trw.StoreTemplate(templatePath, templateFileName, template);
        }

        public virtual void StoreTemplate(string templatePath, GestureTemplate template)
        {
            trw.StoreTemplate(templatePath, template);
        }

        public virtual GestureClass[] GetGestureClasses()
        {
            List<GestureClass> templateList = new List<GestureClass>();
            foreach (var classifier in classifiers)
            {
                if (classifier is StandardTemplateLoader)
                {
                    foreach (GestureClass template
                        in (classifier as StandardTemplateLoader).GetGestureClasses())
                    {
                        templateList.Add(template);
                    }
                    return templateList.ToArray();
                }
            }
            return templateList.ToArray();
        }

        #endregion

        #endregion

        #region private members

        /// <summary>
        /// Gets a value indicating whether recognizer actually is classifying.
        /// </summary>
        /// <value><c>true</c> if [recognition mode]; otherwise, <c>false</c>.</value>
        protected virtual bool RecognitionMode { get; set; }

        protected virtual bool RecordingMode { get; set; }

        #endregion

    }
}