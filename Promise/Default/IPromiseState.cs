using System;

public interface IPromiseState 
{
	Action<IRejectable, Action, Action<Exception>,Exception> Invoke(IRejectable resultPromise, Action resolveHandler, Action<Exception> rejectHandler, Exception rejectionException);
}


