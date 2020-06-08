//---------------------------------------------------------------------------
//
// <copyright file="ObjectAnimationBase.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// This file was generated, please do not edit it directly.
//
// Please see http://wiki/default.aspx/Microsoft.Projects.Avalon/MilCodeGen.html for more information.
//
//---------------------------------------------------------------------------

// Allow use of presharp: #pragma warning suppress <nnnn>
#pragma warning disable 1634, 1691

using MS.Internal;

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.Animation;   
using System.Windows.Media.Media3D;

using MS.Internal.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{       
    /// <summary>
    ///
    /// </summary>
    public abstract class ObjectAnimationBase : AnimationTimeline
    {
        #region Constructors

        /// <Summary>
        /// Creates a new ObjectAnimationBase.
        /// </Summary>
        protected ObjectAnimationBase()
            : base()
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Creates a copy of this ObjectAnimationBase
        /// </summary>
        /// <returns>The copy</returns>
        public new ObjectAnimationBase Clone()
        {
            return (ObjectAnimationBase)base.Clone();
        }

        #endregion

        #region IAnimation

        /// <summary>
        /// Calculates the value this animation believes should be the current value for the property.
        /// </summary>
        /// <param name="defaultOriginValue">
        /// This value is the suggested origin value provided to the animation
        /// to be used if the animation does not have its own concept of a
        /// start value. If this animation is the first in a composition chain
        /// this value will be the snapshot value if one is available or the
        /// base property value if it is not; otherise this value will be the 
        /// value returned by the previous animation in the chain with an 
        /// animationClock that is not Stopped.
        /// </param>
        /// <param name="defaultDestinationValue">
        /// This value is the suggested destination value provided to the animation
        /// to be used if the animation does not have its own concept of an
        /// end value. This value will be the base value if the animation is
        /// in the first composition layer of animations on a property; 
        /// otherwise this value will be the output value from the previous 
        /// composition layer of animations for the property.
        /// </param>
        /// <param name="animationClock">
        /// This is the animationClock which can generate the CurrentTime or
        /// CurrentProgress value to be used by the animation to generate its
        /// output value.
        /// </param>
        /// <returns>
        /// The value this animation believes should be the current value for the property.
        /// </returns>
        public override sealed object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            ReadPreamble();

            if (animationClock == null)
            {
                throw new ArgumentNullException("animationClock");
            }

            // We check for null above but presharp doesn't notice so we suppress the 
            // warning here.

            #pragma warning suppress 6506
            if (animationClock.CurrentState == ClockState.Stopped)
            {
                return defaultDestinationValue;
            }

            /*
            if (!AnimatedTypeHelpers.IsValidAnimationValueObject(defaultDestinationValue))
            {
                throw new ArgumentException(
                    SR.Get(
                        SRID.Animation_InvalidBaseValue,
                        defaultDestinationValue, 
                        defaultDestinationValue.GetType(), 
                        GetType()),
                        "defaultDestinationValue");
            }
            */

            return GetCurrentValueCore(defaultOriginValue, defaultDestinationValue, animationClock);
        }

        /// <summary>
        /// Returns the type of the target property
        /// </summary>
        public override sealed Type TargetPropertyType
        {
            get
            {
                ReadPreamble();

                return typeof(Object);
            }
        }

        #endregion

        #region Methods



        /// <summary>
        /// Calculates the value this animation believes should be the current value for the property.
        /// </summary>
        /// <param name="defaultOriginValue">
        /// This value is the suggested origin value provided to the animation
        /// to be used if the animation does not have its own concept of a
        /// start value. If this animation is the first in a composition chain
        /// this value will be the snapshot value if one is available or the
        /// base property value if it is not; otherise this value will be the 
        /// value returned by the previous animation in the chain with an 
        /// animationClock that is not Stopped.
        /// </param>
        /// <param name="defaultDestinationValue">
        /// This value is the suggested destination value provided to the animation
        /// to be used if the animation does not have its own concept of an
        /// end value. This value will be the base value if the animation is
        /// in the first composition layer of animations on a property; 
        /// otherwise this value will be the output value from the previous 
        /// composition layer of animations for the property.
        /// </param>
        /// <param name="animationClock">
        /// This is the animationClock which can generate the CurrentTime or
        /// CurrentProgress value to be used by the animation to generate its
        /// output value.
        /// </param>
        /// <returns>
        /// The value this animation believes should be the current value for the property.
        /// </returns>
        protected abstract Object GetCurrentValueCore(Object defaultOriginValue, Object defaultDestinationValue, AnimationClock animationClock);

        #endregion
    }
}