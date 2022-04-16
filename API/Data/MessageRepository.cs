using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            //return await _context.Messages.FindAsync(id);
            return await _context.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        // public async Task<PagedList<MessageDTO>> GetMessagesForUser(MessageParams messageParams)
        // {
        //     var query = _context.Messages
        //         .OrderByDescending(m => m.MessageSent)
        //         .AsQueryable();

        //     query = messageParams.Container switch
        //     {
        //         "Inbox" => query.Where(u => u.Recipient.UserName == messageParams.Username 
        //             && u.RecipientDeleted == false),
        //         "Outbox" => query.Where(u => u.Sender.UserName == messageParams.Username
        //             && u.SenderDeleted == false),
        //         _ => query.Where(u => u.Recipient.UserName == 
        //             messageParams.Username 
        //             && u.DateRead == null
        //             && u.RecipientDeleted == false)
        //     };

        // Optimize the GetMessagesForUser method.
        public async Task<PagedList<MessageDTO>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages
                .OrderByDescending(m => m.MessageSent)
                .ProjectTo<MessageDTO>(_mapper.ConfigurationProvider)
                .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.RecipientUsername == messageParams.Username 
                    && u.RecipientDeleted == false),
                "Outbox" => query.Where(u => u.SenderUsername == messageParams.Username
                    && u.SenderDeleted == false),
                _ => query.Where(u => u.RecipientUsername == 
                    messageParams.Username 
                    && u.DateRead == null
                    && u.RecipientDeleted == false)
            };

            return await PagedList<MessageDTO>.CreateAsync(query, messageParams.PageNumber, messageParams.PageSize);
        }

        //     var messages = query.ProjectTo<MessageDTO>(_mapper.ConfigurationProvider);

        //     return await PagedList<MessageDTO>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        // }

        // public async Task<IEnumerable<MessageDTO>> GetMessageThread(string currentUsername, 
        //     string recipientUsermame)
        // {
        //     var messages = await _context.Messages
        //         .Include(u => u.Sender).ThenInclude(p => p.Photos)
        //         .Include(u => u.Recipient).ThenInclude(p => p.Photos)
        //         .Where(m => m.Recipient.UserName == currentUsername && m.RecipientDeleted == false
        //                 && m.Sender.UserName == recipientUsermame
        //                 || m.Recipient.UserName == recipientUsermame
        //                 && m.Sender.UserName == currentUsername && m.SenderDeleted == false
        //         )
        //         .OrderBy(m => m.MessageSent)
        //         .ToListAsync();

        //     var unreadMessages = messages.Where(m => m.DateRead == null 
        //         && m.Recipient.UserName == currentUsername).ToList();

        //     if (unreadMessages.Any())
        //     {
        //         foreach (var message in unreadMessages)
        //         {
        //             message.DateRead = DateTime.UtcNow;
        //         }
                
        //         // This functionality is implemented in SignalR Hub.
        //         // await _context.SaveChangesAsync();
        //     }

        //     return _mapper.Map<IEnumerable<MessageDTO>>(messages);
        // }

        // Optimize the GetMessageThread method.
        public async Task<IEnumerable<MessageDTO>> GetMessageThread(string currentUsername, 
            string recipientUsermame)
        {
            var messages = await _context.Messages
                .Where(m => m.Recipient.UserName == currentUsername && m.RecipientDeleted == false
                        && m.Sender.UserName == recipientUsermame
                        || m.Recipient.UserName == recipientUsermame
                        && m.Sender.UserName == currentUsername && m.SenderDeleted == false
                )
                .OrderBy(m => m.MessageSent)
                .ProjectTo<MessageDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null 
                && m.RecipientUsername == currentUsername).ToList();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
                
                // This functionality is implemented in SignalR Hub.
                // await _context.SaveChangesAsync();
            }

            return messages;
        }


        // Not needed when using Unit Of Work.
        // public async Task<bool> SaveAllAsync()
        // {
        //     return await _context.SaveChangesAsync() > 0; 
        // }

        // This is using SignalR
        public void AddGroup(Group group)
        {
            _context.Groups.Add(group);
        }

        // This is using SignalR
        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _context.Connections.FindAsync(connectionId);
        }

        // This is using SignalR
        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _context.Groups
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        // This is using SignalR
        public void RemoveConnection(Connection connection)
        {
            _context.Connections.Remove(connection);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _context.Groups
                .Include(c => c.Connections)
                .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }
    }
}