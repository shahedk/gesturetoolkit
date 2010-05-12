﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Framework;

using Framework.Exceptions;
using Framework.Utility;
using Gestures.Feedbacks.TouchFeedbacks;
using System.Windows.Controls;
using Gestures.Rules.RuleValidators;
using Gestures.Feedbacks.GestureFeedbacks;
using System.Windows.Media;
using System.Diagnostics;
using Gestures.Objects;
using Framework.TouchInputProviders;
using Gestures.ReturnTypes;

namespace Framework.GestureEvents
{
    public enum TouchInputType
    {
        Silverlight,
        Surface,
        SmartTableTop
    }

    public class GestureEventManager
    {

        #region Constructor & singleton instance
        internal GestureEventManager()
        {

        }

        #endregion

        #region public methods

        /// <summary>
        /// Adds gesture event to specified UI element
        /// </summary>
        /// <param name="uiElement"></param>
        /// <param name="gestureName"></param>
        /// <param name="handler"></param>
        public void AddEvent(UIElement uiElement, string gestureName, GestureEventHandler handler)
        {
            if (GestureFramework.IsInitialized)
            {
                // Add event request
                EventRequestDirectory.Add(uiElement, gestureName, handler);
            }
            else
            {
                throw new FrameworkException("You need to initialize the framework first!. Call GestureEventManager.Initialize(...) at application startup.");
            }
        }

        /// <summary>
        /// Removes the specified gesture event request for the specified UI element
        /// </summary>
        /// <param name="uiElement"></param>
        /// <param name="gestureName"></param>
        public void RemoveEvent(UIElement uiElement, string gestureName)
        {
            EventRequestDirectory.Remove(uiElement, gestureName);
        }

        #endregion


        internal void SubscribeTouchEvents()
        {
            // First, unsubscribe any exising subscription
            UnSubscribeTouchEvents();

            // Subscribe to events
            TouchInputManager.ActiveTouchProvider.FrameChanged += ActiveHardware_FrameChanged;
            TouchInputManager.ActiveTouchProvider.MultiTouchChanged += ActiveHardware_MultiTouchChanged;
        }

        private void UnSubscribeTouchEvents()
        {
            TouchInputManager.ActiveTouchProvider.FrameChanged -= ActiveHardware_FrameChanged;
            TouchInputManager.ActiveTouchProvider.MultiTouchChanged -= ActiveHardware_MultiTouchChanged;
        }

        const int CommonBehaviourInterval = 3;
        void ActiveHardware_FrameChanged(object sender, FrameInfo frameInfo)
        {
            foreach (var cb in GestureFramework.CommonBehaviors)
            {
                try
                {
                    cb.FrameChanged(frameInfo);
                }
                catch //(Exception ex)
                {
                    // TODO: write to log
                }
            }
        }

        // bool? _enableTouchHistory = null;
        private void ActiveHardware_MultiTouchChanged(Object sender, MultiTouchEventArgs e)
        {
            #region block code
            // 0. Validate all pre-conditions
            /* foreach( pre-conditions )
             *      pre-condition.Validate()
             */

            // 1. Get gestures that match their pre-conditions
            /* foreach( gestures )
             *      if( gesture.IsPreConditionsValid() )        // NOTE: Same pre-condition is cross referenced between multiple gestures, they will be evaluated only once
             *          if( gesture.IsTouchPatternsValid() )    // NOTE: Same shape recognizer should be cross referenced and be validated once for a particular TouchPoint2 object
             *          {
             *              gesture.callback( gesture.BuildReturnObjects() )  // Tips: BuildReturnObjects() can be an Extension method
             *          }
             */
            #endregion

            // Validate pre-conditions
            ValidSetOfTouchPoints availableTouchPoints = e.TouchPoints.Copy();

            //TODO: This is a temporary implementation. Although it works properly 
            // but is not an efficient implement. Consider using RETE algorithm

            //List<Gesture> gestures = EventRequestDirectory.GetGestures(availableTouchPoints.GetUIElements());
            foreach (Gesture gesture in GestureLanguageProcessor.ActiveGestures.Values)
            {
                ValidSetOfPointsCollection touchPointsUsed = ValidateGesture(gesture, availableTouchPoints);

                // Remove the touch points that where used to detect this gesture
                //availableTouchPoints.Remove(touchPointsUsed);
            }
        }

        private ValidSetOfPointsCollection ValidateGesture(Gesture gesture, ValidSetOfTouchPoints availableTouchPoints)
        {
            // Validate pre-conditions
            // TODO: Need to sort by precedence order (when we will implement precedence option)
            ValidSetOfPointsCollection validSets = gesture.PreConditions.Validate(availableTouchPoints);

            if (validSets.Count > 0)
            {
                // Validate gesture rules
                validSets = gesture.Rules.Validate(validSets);

                // TODO: If user requests the gesture for specific UI element, 
                // check which valid set satisfies that 

                // Building return objects for each valid sets
                foreach (var validSetOfPoint in validSets)
                {
                    // Build return objects
                    List<IReturnType> returnObjs = gesture.ReturnTypes.Calculate(validSetOfPoint);

                    // Execute gesture effects, if any
                    if (GestureFramework.GestureFeedbacks.ContainsKey(gesture.Name.ToLower()))
                    {
                        var gestureFeeds = GestureFramework.GestureFeedbacks[gesture.Name.ToLower()];
                        foreach (Type gestureFeedbackTyoe in gestureFeeds)
                        {
                            ExecuteFeedback(gestureFeedbackTyoe, returnObjs);
                        }
                    }

                    // Invoke the callback to notify the subscriber(s)
                    List<EventRequest> eventReqs = EventRequestDirectory.GetRequests(gesture.Name,validSetOfPoint.GetUIElements());
                    foreach (var eventRequest in eventReqs)
                    {
                        eventRequest.EventHandler(eventRequest.UIElement, returnObjs);
                    }
                }
            }

            return validSets;
        }

        private bool IsPointOriginatedOnElement(TouchPoint2 point, UIElement uIElement)
        {
            if (point.Tags.ContainsKey(uIElement))
            {
                return (point.Tags[uIElement] as bool?) == true;
            }
            else
            {
                // Get all elements on the position of the point
                bool result = false;

                //TODO: Find a way to do hit-test in WPF 4.0
#if SILVERLIGHT
                var elements = VisualTreeHelper.FindElementsInHostCoordinates(point.StartPoint.ToPoint(), GestureFramework.LayoutRoot);
                foreach (var uiItem in elements)
                {
                    if (uiItem == uIElement)
                    {
                        result = true;
                        break;
                    }
                }
#else
                // Code block for : .NET Framework 4.0 / WPF 4.0 
#endif
                point.Tags[uIElement] = result;

                return result;
            }
        }

        private void ExecuteFeedback(Type type, List<IReturnType> returnObjects)
        {
            (Activator.CreateInstance(type) as IGestureFeedback).RenderUI(GestureFramework.LayoutRoot.Dispatcher, GestureFramework.LayoutRoot, returnObjects);
        }

    }
}
