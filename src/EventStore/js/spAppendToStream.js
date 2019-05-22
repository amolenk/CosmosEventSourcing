function appendToStream(streamId, expectedVersion, events) {

    var versionQuery = 
    {     
        'query' : 'SELECT Max(e.stream.version) FROM events e WHERE e.stream.id = @streamId',
        'parameters' : [{ 'name': '@streamId', 'value': streamId }] 
    };

    const isAccepted = __.queryDocuments(__.getSelfLink(), versionQuery,
        function (err, items, options) {
            if (err) throw new Error("Unable to get stream version: " + err.message);

            if (!items || !items.length) {
                throw new Error("No results from stream version query.");
            }

            var currentVersion = items[0].$1;

            // Concurrency check.
            if ((!currentVersion && expectedVersion == 0)
                || (currentVersion == expectedVersion))
            {
                // Everything's fine, bulk insert the events.
                JSON.parse(events).forEach(event =>
                    __.createDocument(__.getSelfLink(), event));

                __.response.setBody(true);
            }
            else {
                __.response.setBody(false);
            }
        });

    if (!isAccepted) throw new Error('The query was not accepted by the server.');
}