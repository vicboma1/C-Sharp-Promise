using System;

public struct RejectHandler<Z>  where Z : Exception {
	public Action<Z> callback;
	public IRejectable rejectable;
}