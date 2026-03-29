namespace NiveraAPI.Rest.Client
{
    /// <summary>
    /// Represents an HTTP request object that holds data and state shared during the execution
    /// of an HTTP transaction. Provides methods for managing and validating state.
    /// </summary>
    public class RestRequest
    {
        /// <summary>
        /// Represents the context associated with an HTTP request. This variable is of type <see cref="RestClientContext"/>,
        /// which encapsulates the request, response, and related metadata for an HTTP operation.
        /// The context is used to track the state of an HTTP request and its associated response,
        /// providing functionality to access response data, headers, status, error information, and utilities
        /// for processing response content.
        /// It is declared as a volatile field to ensure thread-safe access and updates
        /// in multithreaded environments where the HTTP requests are processed asynchronously.
        /// </summary>
        public volatile RestClientContext Context;

        /// <summary>
        /// Defines a delegate to handle a callback operation for an HTTP request. This variable is of type <see cref="System.Action{RestRequest}"/>,
        /// allowing for the specification of a custom action that executes when a request-related event occurs.
        /// The callback function is invoked with the associated <see cref="RestRequest"/> instance,
        /// enabling the processing of request-specific actions, such as handling the response or managing errors.
        /// It is declared as a volatile field to ensure that changes to the callback are immediately visible
        /// in a multithreaded environment where asynchronous operations might modify or invoke it.
        /// </summary>
        public volatile Action<RestRequest> Callback;

        /// <summary>
        /// Represents the user-defined state associated with an HTTP request. This variable is of type <see cref="object"/>,
        /// allowing it to hold any custom data or identifier relevant to the HTTP operation.
        /// It can be used to track additional information or metadata that is not inherently managed by the HTTP framework.
        /// The state is often employed for correlating requests with their originating context or for carrying custom user data
        /// through asynchronous workflows.
        /// It is defined as a volatile field to ensure consistent visibility of updates across multiple threads
        /// in concurrent environments.
        /// </summary>
        public volatile object State;

        /// <summary>
        /// Indicates whether the current instance has a non-null state associated with it.
        /// This property evaluates the <c>State</c> field and returns <c>true</c> if a state object
        /// is set, otherwise it returns <c>false</c>. It is used to determine if the instance
        /// maintains any contextual state data for further processing or validation.
        /// </summary>
        public bool HasState => State != null;

        /// <summary>
        /// Determines if the current state matches the expected state, with an optional allowance for both to be null.
        /// </summary>
        /// <param name="expectedState">The state to compare against the current state.</param>
        /// <param name="countIfBothNull">
        /// A flag indicating whether to return true if both the current state and the expected state are null.
        /// </param>
        /// <returns>
        /// True if the current state matches the expected state, or if both are null and the flag countIfBothNull is true; otherwise, false.
        /// </returns>
        public bool IsState(object expectedState, bool countIfBothNull = false)
        {
            if (State == null && expectedState == null)
                return countIfBothNull;
            
            if ((State == null && expectedState != null) || (State != null && expectedState == null))
                return false;
            
            return State.Equals(expectedState);
        }

        /// <summary>
        /// Checks if the current state matches the specified type.
        /// </summary>
        /// <typeparam name="T">The type to compare against the current state.</typeparam>
        /// <returns>
        /// True if the current state matches the specified type; otherwise, false.
        /// </returns>
        public bool IsState<T>()
        {
            return IsState(typeof(T));
        }

        /// <summary>
        /// Determines if the current state matches the provided state type.
        /// </summary>
        /// <param name="stateType">The type to compare against the type of the current state.</param>
        /// <returns>
        /// True if the current state's type matches the provided state type; otherwise, false.
        /// Returns false if the provided state type is null or if the current state is null.
        /// </returns>
        public bool IsState(Type stateType)
        {
            if (stateType == null)
                return false;

            if (!HasState)
                return false;
            
            return State.GetType() == stateType;
        }
    }
}