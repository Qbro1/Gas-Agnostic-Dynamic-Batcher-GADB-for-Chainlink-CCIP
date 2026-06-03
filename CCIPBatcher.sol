// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import {IRouterClient} from "@chainlink/contracts-ccip/src/v0.8/ccip/interfaces/IRouterClient.sol";
import {Client} from "@chainlink/contracts-ccip/src/v0.8/ccip/libraries/Client.sol";
import {IERC20} from "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import {SafeERC20} from "@openzeppelin/contracts/token/ERC20/utils/SafeERC20.sol";


contract CCIPBatcher {
    using SafeERC20 for IERC20;

    // Errors
    error InsufficientFee();
    error InvalidBatchLength();
    error TransferFailed();

    address public immutable i_router;
    address public immutable i_linkToken;

    event BatchSent(bytes32 indexed messageId, uint64 indexed destinationChainSelector, uint256 batchSize);

    constructor(address router, address linkToken) {
        i_router = router;
        i_linkToken = linkToken;
    }

    /**
* @notice Sends a bundle of tokens to different recipients on another network in ONE CCIP transaction.
* @param destinationChainSelector Code of the target network (Chain Selector)
* @param receivers Array of recipient addresses on the target network
* @param amounts Array of amounts for each recipient
* @param token Address of the token being sent (for example, USDC)
*/
    function sendBatchTokens(
        uint64 destinationChainSelector,
        address[] calldata receivers,
        uint256[] calldata amounts,
        address token
    ) external returns (bytes32 messageId) {
        uint256 length = receivers.length;
        if (length == 0 || length != amounts.length) revert InvalidBatchLength();

        //all_amount
        uint256 totalAmount = 0;
        unchecked {
            for (uint256 i = 0; i < length; i++) {
                totalAmount += amounts[i];
            }
        }

        
        IERC20(token).safeTransferFrom(msg.sender, address(this), totalAmount);

        bytes memory payload = abi.encode(receivers, amounts);


        Client.EVMTokenAmount[] memory tokenAmounts = new Client.EVMTokenAmount[](1);
        tokenAmounts[0] = Client.EVMTokenAmount({
            token: token,
            amount: totalAmount
        });

        
        Client.EVM2AnyMessage memory ccipMessage = Client.EVM2AnyMessage({
            receiver: abi.encode(msg.sender), 
            data: payload,
            tokenAmounts: tokenAmounts,
            extraArgs: Client._argsToBytes(
                Client.EVMExtraArgsV1({gasLimit: 300_000}) 
            ),
            feeToken: i_linkToken
        });

        uint256 fees = IRouterClient(i_router).getFee(destinationChainSelector, ccipMessage);
        if (fees > IERC20(i_linkToken).balanceOf(msg.sender)) revert InsufficientFee();

        
        IERC20(i_linkToken).safeTransferFrom(msg.sender, address(this), fees);
        IERC20(i_linkToken).approve(i_router, fees);
        IERC20(token).approve(i_router, totalAmount);

       
        messageId = IRouterClient(i_router).ccipSend(destinationChainSelector, ccipMessage);

        emit BatchSent(messageId, destinationChainSelector, length);
    }
}
