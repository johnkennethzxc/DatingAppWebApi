using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        // private readonly IUserRepository _userRepository;
        // private readonly IMessageRepository _messageRepository;
        // private readonly IMapper _mapper;
        // public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
        // {
        //     _mapper = mapper;
        //     _userRepository = userRepository;
        //     _messageRepository = messageRepository;
        // }
        

        // This is using Unit Of Work.
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public MessagesController(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // This functionality is implemented in SignalR Hub
        // [HttpPost]
        // public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO)
        // {
        //     var username = User.GetUsername();

        //     if (username == createMessageDTO.RecipientUsername.ToLower()) 
        //         return BadRequest("You cannot send messages to yourself");

        //     // var sender = await _userRepository.GetUserByUsernameAsync(username);
        //     // var recipient = await _userRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);

        //     // This is using Unit Of Work.
        //     var sender = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
        //     var recipient = await _unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);

        //     if (recipient == null) return NotFound();

        //     var message = new Message
        //     {
        //         Sender = sender,
        //         Recipient = recipient,
        //         SenderUsername = sender.UserName,
        //         RecipientUsername = recipient.UserName,
        //         Content = createMessageDTO.Content
        //     };

        //     // _messageRepository.AddMessage(message);

        //     // This is using Unit Of Work.
        //     _unitOfWork.MessageRepository.AddMessage(message);

        //     // if (await _messageRepository.SaveAllAsync()) return Ok(_mapper.Map<MessageDTO>(message));

        //     // This is using Unit Of Work.
        //     if (await _unitOfWork.Complete()) return Ok(_mapper.Map<MessageDTO>(message));

        //     return BadRequest("Failed to send message");
        // }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessagesForUser([FromQuery] 
            MessageParams messageParams)
        {
            messageParams.Username = User.GetUsername();
            // var messages = await _messageRepository.GetMessagesForUser(messageParams);

            // This is using Unit Of Work
            var messages = await _unitOfWork.MessageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, 
                messages.TotalCount, messages.TotalPages);

            return messages;
        }

        // This functionality is implemented in the SignalR Hub.
        // [HttpGet("thread/{username}")]
        // public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread(string username)
        // {
        //     var currentUsername = User.GetUsername();

        //     return Ok(await _messageRepository.GetMessageThread(currentUsername, username));
        // }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();

            // var message = await _messageRepository.GetMessage(id);

            // This is using Unit Of Work.
            var message = await _unitOfWork.MessageRepository.GetMessage(id);

            if (message.Sender.UserName != username && message.Recipient.UserName != username) 
                return Unauthorized();

            if (message.Sender.UserName == username) message.SenderDeleted = true;

            if (message.Recipient.UserName == username) message.RecipientDeleted = true;

            // if (message.SenderDeleted && message.RecipientDeleted) _messageRepository.DeleteMessage(message);
            
            // This is using Unit Of Work.
            if (message.SenderDeleted && message.RecipientDeleted) _unitOfWork.MessageRepository.DeleteMessage(message);

            // if (await _messageRepository.SaveAllAsync()) return Ok();

            // This is using Unit Of Work.
            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Problem when deleting a message");
        }
    }
}