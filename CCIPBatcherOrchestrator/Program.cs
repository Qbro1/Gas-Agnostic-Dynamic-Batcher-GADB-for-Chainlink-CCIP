using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace CCIPBatcherOrchestrator
{
    class Program
    {
        
        private static readonly string ContractAbi = @"[
            {
                ""inputs"": [
                    {""internalType"": ""uint64"", ""name"": ""destinationChainSelector"", ""type"": ""uint64""},
                    {""internalType"": ""address[]"", ""name"": ""receivers"", ""type"": ""address[]""},
                    {""internalType"": ""uint256[]"", ""name"": ""amounts"", ""type"": ""uint256[]""},
                    {""internalType"": ""address"", ""name"": ""token"", ""type"": ""address""}
                ],
                ""name"": ""sendBatchTokens"",
                ""outputs"": [{""internalType"": ""bytes32"", ""name"": ""messageId"", ""type"": ""bytes32""}],
                ""stateMutability"": ""nonpayable"",
                ""type"": ""function""
            }
        ]";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== STARTING C# CCIP BATCH ORCHESTRATOR ===");

            
            string rpcUrl = "https://sepolia.arbitrum.io/rpc";
            string privateKey = "YOUR_PRIVATE_KEY_HERE";
            string contractAddress = "YOUR_CONTRACT_ADDRESS_HERE";
            string tokenAddress = "0x75faf114eafb1BDbe2F0316DF893fd58CE46AA4d";
            ulong destinationChainSelector = 16015286601757825753;

            var account = new Account(privateKey);
            var web3 = new Web3(account, rpcUrl);

            
            var txQueue = new List<TransactionIntent>
            {
                new TransactionIntent("0x1111111111111111111111111111111111111111", Web3.Convert.ToWei(10)),
                new TransactionIntent("0x2222222222222222222222222222222222222222", Web3.Convert.ToWei(15)),
                new TransactionIntent("0x3333333333333333333333333333333333333333", Web3.Convert.ToWei(25))
            };

            if (txQueue.Count >= 3)
            {
                Console.WriteLine($"Queue is full ({txQueue.Count} transactions). Checking network gas...");

                var currentGasPrice = await web3.Eth.GasPrice.SendRequestAsync();
                Console.WriteLine($"Current Gas Price: {currentGasPrice.Value} wei");

                var receivers = new List<string>();
                var amounts = new List<BigInteger>();

                foreach (var intent in txQueue)
                {
                    receivers.Add(intent.Receiver);
                    amounts.Add(intent.Amount);
                }

                Console.WriteLine("Building and sending batch transaction...");

                try
                {
                    var contract = web3.Eth.GetContract(ContractAbi, contractAddress);
                    var sendBatchFunction = contract.GetFunction("sendBatchTokens");

                 
                    object[] functionParams = new object[]
                    {
                        destinationChainSelector,
                        receivers.ToArray(),
                        amounts.ToArray(),
                        tokenAddress
                    };

                    
                    var transactionData = sendBatchFunction.GetData(functionParams);

                   
                    var transactionInput = new TransactionInput
                    {
                        From = account.Address,
                        To = contractAddress,
                        Data = transactionData,
                        GasPrice = currentGasPrice
                    };

                  
                    var gasEstimation = await sendBatchFunction.EstimateGasAsync(
                        account.Address,
                        null,
                        null,
                        destinationChainSelector,
                        receivers.ToArray(),
                        amounts.ToArray(),
                        tokenAddress
                    );

                    Console.WriteLine($"Gas estimation: {gasEstimation.Value} units");

                 
                    transactionInput.Gas = gasEstimation;

                    var txHash = await web3.Eth.TransactionManager.SendTransactionAsync(transactionInput);

                    Console.WriteLine($" Batch sent! Transaction hash: {txHash}");
                    Console.WriteLine("Clearing buffer queue.");
                    txQueue.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending batch: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }
        }
    }

    public class TransactionIntent
    {
        public string Receiver { get; set; }
        public BigInteger Amount { get; set; }

        public TransactionIntent(string receiver, BigInteger amount)
        {
            Receiver = receiver;
            Amount = amount;
        }
    }
}