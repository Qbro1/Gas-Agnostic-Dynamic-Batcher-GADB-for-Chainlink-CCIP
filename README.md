# Gas-Agnostic-Dynamic-Batcher-GADB-for-Chainlink-CCIP

A fully operational production-grade cross-chain aggregation layer designed to bundle single token transfers into optimized atomic batch messages via Chainlink CCIP.

The system consists of two major layers:
1. **Solidity Execution Layer (`/contracts`)**: An gas-optimized smart contract utilizing custom errors, cached state arrays, and unchecked loops to guarantee minimal execution cost.
2. **.NET C# Orchestration Layer (`/service`)**: A high-performance off-chain service that monitors transaction flows, processes queuing logic, and dynamically targets optimal network gas pricing before packing and broadcasting transactions.

 How To Run (Foundry Deployment)

 Compile Contracts
```bash
forge build

```
 Deploy Contract
```bash
forge create --rpc-url <YOUR_RPC_URL> \
  --private-key <YOUR_PRIVATE_KEY> \
  src/CCIPBatcher.sol:CCIPBatcher \
  --constructor-args <CHAINLINK_ROUTER_ADDRESS> <LINK_TOKEN_ADDRESS>

```
 Run C# Orchestrator Service
Update config parameters in Program.cs and launch the service:
```bash
cd service
dotnet run

```
##  Author
Qbro
```
