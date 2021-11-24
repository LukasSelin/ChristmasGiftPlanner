using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ChristmasGiftPlanner.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace ChristmasGiftPlanner.Service;

public class WhishlistService
{
    private static readonly string Ip = "localhost";
    private static readonly int Port = 8000;
    private static readonly string EndpointUrl = "http://" + Ip + ":" + Port;
    private static AmazonDynamoDBClient Client;

    public static Table Table { get; private set; }

    private static AmazonDynamoDBClient MoviesTable;
    

    private static bool IsPortInUse()
    {
        bool isAvailable = true;
        // Evaluate current system TCP connections. This is the same information provided
        // by the netstat command line application, just in .Net strongly-typed object
        // form.  We will look through the list, and if our port we would like to use
        // in our TcpClient is occupied, we will set isAvailable to false.
        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
        foreach (IPEndPoint endpoint in tcpConnInfoArray)
        {
            if (endpoint.Port == Port)
            {
                isAvailable = false;
                break;
            }
        }

        return isAvailable;
    }

    public static bool createClient(bool useDynamoDbLocal)
    {
        if (useDynamoDbLocal)
        {
            // First, check to see whether anyone is listening on the DynamoDB local port
            // (by default, this is port 8000, so if you are using a different port, modify this accordingly)
           

            // DynamoDB-Local is running, so create a client
            Console.WriteLine("  -- Setting up a DynamoDB-Local client (DynamoDB Local seems to be running)");
            AmazonDynamoDBConfig ddbConfig = new AmazonDynamoDBConfig();
            ddbConfig.ServiceURL = EndpointUrl;
            try
            {
                Client = new AmazonDynamoDBClient("AKIA4DY25FZ2A54V6VVF", "RjYyIP3QEvlashOsztsUHRrILARkRpxlegHxQvlV", ddbConfig);
                Table = Table.LoadTable(Client, "Whishlists");
            }
            catch (Exception ex)
            {
                Console.WriteLine("     FAILED to create a DynamoDBLocal client; " + ex.Message);
                return false;
            }
        }
        else
        {
            Client = new AmazonDynamoDBClient();
        }

        return true;
    }
    public static async Task<bool> CheckingTableExistence_async(string tblNm)
    {
        var response = await WhishlistService.Client.ListTablesAsync();
        return response.TableNames.Contains(tblNm);
    }

    public static async Task<bool> CreateTable_async(string tableName,
        List<AttributeDefinition> tableAttributes,
        List<KeySchemaElement> tableKeySchema,
        ProvisionedThroughput provisionedThroughput)
    {
        bool response = true;

        // Build the 'CreateTableRequest' structure for the new table
        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = tableAttributes,
            KeySchema = tableKeySchema,
            // Provisioned-throughput settings are always required,
            // although the local test version of DynamoDB ignores them.
            ProvisionedThroughput = provisionedThroughput
        };

        try
        {
            var makeTbl = await WhishlistService.Client.CreateTableAsync(request);
        }
        catch (Exception)
        {
            response = false;
        }

        return response;
    }

    public static async Task<TableDescription> GetTableDescription(string tableName)
    {
        TableDescription result = null;

        // If the table exists, get its description.
        try
        {
            var response = await WhishlistService.Client.DescribeTableAsync(tableName);
            result = response.Table;
        }
        catch (Exception)
        { }

        return result;
    }
    public static async Task<bool> LoadingData_async(Table table, string filePath)
    {
        var WhishlistArray = await ReadJsonWhishlistFile_async(filePath);

        if (WhishlistArray != null)
            await LoadJsonWhishlistData_async(table, WhishlistArray);

        return true;
    }

    public static async Task<JArray> ReadJsonWhishlistFile_async(string jsonWhishlistFilePath)
    {
        StreamReader sr = null;
        JsonTextReader jtr = null;
        JArray WhishlistArray = null;

        Console.WriteLine("  -- Reading the Whishlists data from a JSON file...");

        try
        {
            sr = new StreamReader(jsonWhishlistFilePath);
            jtr = new JsonTextReader(sr);
            WhishlistArray = (JArray)await JToken.ReadFromAsync(jtr);
        }
        catch (Exception ex)
        {
            Console.WriteLine("     ERROR: could not read the file!\n          Reason: {0}.", ex.Message);
        }
        finally
        {
            jtr?.Close();
            sr?.Close();
        }

        return WhishlistArray;
    }

    public static async Task<bool> LoadJsonWhishlistData_async(Table WhishlistsTable, JArray WhishlistsArray)
    {
        int n = WhishlistsArray.Count;
        Console.Write("     -- Starting to load {0:#,##0} Whishlist records into the Whishlists table asynchronously...\n" + "" +
          "        Wrote: ", n);
        for (int i = 0, j = 99; i < n; i++)
        {
            try
            {
                string itemJson = WhishlistsArray[i].ToString();
                Document doc = Document.FromJson(itemJson);
                Task putItem = WhishlistsTable.PutItemAsync(doc);
                if (i >= j)
                {
                    j++;
                    Console.Write("{0,5:#,##0}, ", j);
                    if (j % 1000 == 0)
                        Console.Write("\n               ");
                    j += 99;
                }
                await putItem;
            }
            catch (Exception)
            {
                return false;
            }
        }

        return true;
    }
    public static async Task<bool> CheckingForWhishlist_async(Document newItem)
    {
        string name = newItem["name"];

        var response = await ReadingWhishlist_async(name);

        return response.Count > 0;
    }

    public static async Task<bool> WritingNewWhishlist_async(Document newItem)
    {
        var result = false;

        try
        {
            var writeNew = await Table.PutItemAsync(newItem);
            Console.WriteLine("  -- Writing a new Whishlist to the Whishlists table...");

            Console.WriteLine("      -- Wrote the item successfully!");
            result = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("      FAILED to write the new Whishlist, because:\n       {0}.", ex.Message);
        }

        return result;
    }
    public static async Task<Document> ReadingWhishlist_async(string UserName, CancellationToken cancellationToken = default)
    {
        // Create Primitives for the HASH and RANGE portions of the primary key
        Primitive hash = new Primitive(UserName, false);
        var key = new Dictionary<string, AttributeValue>()
        {
            { "Whishlists" , new AttributeValue(UserName) }
        };

        try
        {
            var movieItem = await Table.GetItemAsync(hash, cancellationToken);
            return movieItem;
        }
        catch (Exception)
        {
            return null;
        }
    }
    public static async Task<bool> UpdatingMovie_async(UpdateItemRequest updateRequest)
    {
        var result = false;

        try
        {
            await Client.UpdateItemAsync(updateRequest);
            result = true;
        }
        catch (Exception)
        {
            result = false;
        }

        return result;
    }
}

