using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "This is not marked as static because we want people to be able to derive from it")]
public class Record
{
	[SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This parameter is verified elsewhere.")]
	public static Exception Exception(Assert.ThrowsDelegate code)
	{
		try
		{
			code();
			return null;
		}
		catch (Exception ex)
		{
			return ex;
		}
	}

	[SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This parameter is verified elsewhere.")]
	public static Exception Exception(Assert.ThrowsDelegateWithReturn code)
	{
		try
		{
			code();
			return null;
		}
		catch (Exception ex)
		{
			return ex;
		}
	}
}
