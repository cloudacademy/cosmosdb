using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public class Program
{
    // Replace <documentEndpoint> with the information created earlier
    private static readonly string EndpointUri = "<Endpoint URI>";

    // Set variable to the Primary Key from earlier.
    private static readonly string PrimaryKey = "<Primary Key>";

    // The names of the database and container we will create
    private string databaseId = "ProductDatabase";
    private string containerId = "ProductContainer";

    // C# record representing an item in the container
    public record Product(
        string id,
        string categoryId,
        string categoryName,
        string name,
        int quantity
    );


    public static async Task Main(string[] args)
    {
        try
        {
            Program p = new Program();
            await p.CosmosAsync();

        }
        catch (CosmosException de)
        {
            Exception baseException = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e);
        }
    }


    public async Task CosmosAsync()
    {
        // Create a new instance of the Cosmos Client
        CosmosClient cosmosClient = new(EndpointUri, PrimaryKey);
        
        // Create a new database using the cosmosClient
        Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
        Console.WriteLine("Created Database: {0}\n", database.Id);

        // Create a new container
        Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/categoryId");
        Console.WriteLine("Created Container: {0}\n", container.Id);

        // Create a product item
        Product newItem = new(
            id: "937529657309",
            categoryId: "57385",
            categoryName: "appliances",
            name: "Toaster Oven",
            quantity: 15
        );

        // Insert item in container
        Product createdItem = await container.CreateItemAsync<Product>(
            item: newItem,
            partitionKey: new PartitionKey("57385")
        );
        Console.WriteLine($"Created item:\t{createdItem.id}\t{createdItem.name}\n");

        // Read item from container using the id and the partition key
        Product readItem = await container.ReadItemAsync<Product>(
            id: "937529657309",
            partitionKey: new PartitionKey("57385")
        );
        Console.WriteLine($"Read item:\t{readItem.id}\t{readItem.name}\n");

        // Create query using a SQL string and parameters
        var query = new QueryDefinition(
            query: "SELECT * FROM c WHERE c.categoryId = @categoryId"
        )
            .WithParameter("@categoryId", "57385");

        using FeedIterator<Product> feed = container.GetItemQueryIterator<Product>(
            queryDefinition: query
        );

        while (feed.HasMoreResults)
        {
            FeedResponse<Product> response = await feed.ReadNextAsync();
            foreach (Product item in response)
            {
                Console.WriteLine($"Found item:\t{item.id}\t{item.name}\n");
            }
        }
    }
}