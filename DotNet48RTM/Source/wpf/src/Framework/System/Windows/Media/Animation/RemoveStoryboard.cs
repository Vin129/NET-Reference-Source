/***************************************************************************\
*
* File: RemoveStoryboard.cs
*
* This object includes a Storyboard reference.  When triggered, the Storyboard
*  stops.
*
* Copyright (C) by Microsoft Corporation.  All rights reserved.
*
\***************************************************************************/
using System.Diagnostics;               // Debug.Assert

namespace System.Windows.Media.Animation
{
/// <summary>
/// RemoveStoryboard will call remove on its Storyboard reference when
///  it is triggered.
/// </summary>
public sealed class RemoveStoryboard : ControllableStoryboardAction
{
    /// <summary>
    ///     Called when it's time to execute this storyboard action
    /// </summary>
    internal override void Invoke( FrameworkElement containingFE, FrameworkContentElement containingFCE, Storyboard storyboard )
    {
        Debug.Assert( containingFE != null || containingFCE != null,
            "Caller of internal function failed to verify that we have a FE or FCE - we have neither." );

        if( containingFE != null )
        {
            storyboard.Remove(containingFE);
        }
        else
        {
            storyboard.Remove(containingFCE);
        }
    }
}
}
