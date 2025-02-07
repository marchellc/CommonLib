using System;

namespace CommonLib.Http.Client;

public class HttpRequest
{
    public volatile HttpContext Context;
    public volatile Action<HttpRequest> Callback;

    public volatile object State;
    
    public bool HasState => State != null;

    public bool IsState(object expectedState, bool countIfBothNull = false)
    {
        if (State is null && expectedState is null)
            return countIfBothNull;
        else if ((State is null && expectedState != null) || (State != null && expectedState is null))
            return false;
        else
            return State.Equals(expectedState);
    }

    public bool IsState<T>()
        => IsState(typeof(T));
    
    public bool IsState(Type stateType)
    {
        if (stateType is null)
            return false;

        if (!HasState)
            return false;
        
        return State.GetType() == stateType;
    }
}