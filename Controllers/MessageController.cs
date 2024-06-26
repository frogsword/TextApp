﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using TextApp.Data;
using TextApp.Dtos.MessageDtos;
using TextApp.Hubs;
using TextApp.Interfaces;
using TextApp.Mappers;
using TextApp.Models;
using TextApp.Services;

namespace TextApp.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessageInterface _messageRepo;
        private readonly IHubContext<TextHub> _hub;

        public MessageController
        (
            IMessageInterface messageRepo,
            IHubContext<TextHub> hub
        )
        {
            _messageRepo = messageRepo;
            _hub = hub;
        }

        [HttpGet]
        [Route("{groupId:guid}")]
        [Authorize]
        //[OutputCache]
        public async Task<IActionResult> GetMessages([FromRoute] string groupId)
        {
            var guidGroupId = new Guid( groupId );
            var messages = await _messageRepo.GetAsync(guidGroupId);

            return Ok(messages);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateMessageDto createMessageDto)
        {
            if (ModelState.IsValid)
            {
                var messageModel = createMessageDto.ToMessageFromCreateDto();
                var message = await _messageRepo.CreateAsync(messageModel);

                await _hub.Clients.Group(message.GroupId.ToString()).SendAsync("ReceiveMessage", message);

                return Ok(message);
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [HttpPut("{messageId:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateMessage([FromRoute] Guid messageId, [FromBody] string body)
        {
            if (ModelState.IsValid)
            {
                var messages = await _messageRepo.UpdateAsync(messageId, body);

                if (messages.Count > 0)
                {
                    await _hub.Clients.Group(messages[0].GroupId.ToString()).SendAsync("UpdateGroupMessages", messages);

                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [HttpDelete("{messageId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteMessage([FromRoute] Guid messageId)
        {
            var messages = await _messageRepo.DeleteAsync(messageId);

            if (messages.Count > 0)
            {
                await _hub.Clients.Group(messages[0].GroupId.ToString()).SendAsync("UpdateGroupMessages", messages);

                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
