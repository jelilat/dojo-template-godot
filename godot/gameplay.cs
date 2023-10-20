using Godot;
using System;
using StarkSharp.Rpc.Utils;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarkSharp.Rpc;
using StarkSharp.StarkCurve.Signature;

public partial class gameplay : Sprite2D
{
	private string systemAddress = "SYSTEM_ADDRESS";
	private string playerAddress = "PLAYER_ADDRESS";
	private string playerKey = "PLAYER_PRIVATE_KEY";

	// This enum represents the movement directions.
	enum Direction
	{
		Left = 1,
		Right = 2,
		Up = 3,
		Down = 4,
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		float speed = 20;
		Direction? direction = null;
		if (Input.IsActionPressed("ui_right"))
		{
			Position += new Vector2(speed, 0);
			direction = Direction.Right;
		}
		if (Input.IsActionPressed("ui_left"))
		{
			Position += new Vector2(-speed, 0);
			direction = Direction.Left;
		}
		if (Input.IsActionPressed("ui_up"))
		{
			Position += new Vector2(0, -speed);
			direction = Direction.Up;
		}
		if (Input.IsActionPressed("ui_down"))
		{
			Position += new Vector2(0, speed);
			direction = Direction.Down;
		}

		if (direction != null)
		{
			Execute("move", new string[] { ((int)direction).ToString("X") });
		}
	}

	public void Spawn()
	{
		gameplay instance = new gameplay();
		instance.Execute("spawn", Array.Empty<string>());
	}

	// Executes a function on the contract
	void Execute(string model, string[] args)
	{
		int cairoVersion = 0;
		string maxFee = "0x0"; // set to 0 since fee is disabled on KATANA
		string chainId = "0x4b4154414e41"; // KATANA

		CreateTransaction(playerAddress, systemAddress, model, args, cairoVersion, maxFee, chainId, playerKey);
	}

	// Gets data from the contract
	private object GetData(string model, string[] args)
	{
		object requestData = new
		{
			contract_address = systemAddress,
			entry_point_selector = StarknetOps.CalculateFunctionSelector(model),
			calldata = args,
		};

		object response = SendRequest("starknet_call", requestData);
		GD.Print("Response: " + response);

		return response;
	}

	// Transaction

	public class Request
	{
		public string type { get; set; }
		public string sender_address { get; set; }
		public string[] calldata { get; set; }
		public string max_fee { get; set; }
		public string version { get; set; }
		public string[] signature { get; set; }
		public string nonce { get; set; }
	}
	public async void CreateTransaction(string senderAddress, string contractAddress, string functionName, string[] functionArgs, int cairoVersion, string maxFee, string chainId, string privateKey)
	{

		string functionNameSelector = StarknetOps.CalculateFunctionSelector(functionName);
		TransactionHash.Call[] callArray = new TransactionHash.Call[] { new TransactionHash.Call { To = contractAddress, Selector = functionNameSelector, Data = functionArgs } };
		string calldataHash = "0x" + TransactionHash.Hash.ComputeCalldataHash(callArray, cairoVersion);
		string[] calldata = TransactionHash.Hash.FormatCalldata(callArray, cairoVersion);

		try
		{
			string[] requestParams = new string[]
			{
				"latest",
				senderAddress
			};

			var response = await SendRequest("starknet_getNonce", requestParams);

			var rpcResponse = JsonConvert.DeserializeObject<JsonRpcResponse>(response.Item2);
			var nonce = rpcResponse.result;

			ECDSA.ECSignature signature = TransactionHash.Hash.SignInvokeTransaction(
					"0x1",
					senderAddress,
					calldataHash,
					maxFee,
					chainId,
					nonce.ToString(),
					TransactionHash.Hash.HexToBigInteger(privateKey)
				);

			string r = TransactionHash.Hash.BigIntegerToHex(signature.R);
			string s = TransactionHash.Hash.BigIntegerToHex(signature.S);

			Request[] transactionRequest = new Request[]
			{
				new Request
				{
					type = "INVOKE",
					sender_address = senderAddress,
					calldata = calldata,
					max_fee = maxFee,
					version = "0x1",
					signature = new string[] { r, s },
					nonce = nonce.ToString()
				}
			};
			response = await SendRequest("starknet_addInvokeTransaction", transactionRequest);

			GD.Print("Transaction completed: " + response);
		}
		catch (Exception ex)
		{
			GD.PrintErr("An error occurred: " + ex.Message);
		}
	}

	// Rpc

	private string _hostUrl = "http://localhost:5050"; // Replace with your Katana rpc URL
	private TaskCompletionSource<(int, string)> _tcs; // Used to hold and return the response

	public Task<(int, string)> SendRequest(string method, object parameters)

	{
		_tcs = new TaskCompletionSource<(int, string)>(); // Initialize the TaskCompletionSource

		// Construct the RPC request payload.
		var request = new
		{
			jsonrpc = "2.0",
			method,
			@params = parameters,
			id = 0,
		};

		// Serialize the request object to JSON.
		string jsonRequest = JsonConvert.SerializeObject(request);

		// Create an HTTPRequest instance.
		HttpRequest httpRequest = new HttpRequest();
		this.AddChild(httpRequest);  // Add httpRequest to the current scene.
		httpRequest.RequestCompleted += HttpRequestCompleted;  // Subscribe to the event.

		GD.Print("Request: " + jsonRequest);
		// Start the request.
		var error = httpRequest.Request(_hostUrl, new string[] { "Content-Type: application/json" }, HttpClient.Method.Post, jsonRequest);

		if (error != Error.Ok)
		{
			_tcs.SetException(new Exception("Request failed: " + error)); // Set the exception if there's an error
		}

		return _tcs.Task; // Return the task, which will complete when _tcs is set
	}

	private void HttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		string responseBody = body != null ? System.Text.Encoding.UTF8.GetString(body) : string.Empty;

		// Trigger the event with the response data.
		_tcs.SetResult(((int)responseCode, responseBody)); // Assuming your _tcs is of type TaskCompletionSource<(long, string)>

		// Clean up the HttpRequest node.
		GetNode("HttpRequest")?.QueueFree();
	}
}
