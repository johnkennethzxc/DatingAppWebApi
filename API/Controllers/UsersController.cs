using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        // private readonly DataContext _context;
        // public UsersController(DataContext context)
        // {
        //     _context = context;
        // }
        // private readonly IMapper _mapper;
        // private readonly IPhotoService _photoService;
        // private readonly IUserRepository _userRepository;
        // public UsersController(IUserRepository userRepository, IMapper mapper, 
        //     IPhotoService photoService)
        // {
        //     _photoService = photoService;
        //     _mapper = mapper;
        //     _userRepository = userRepository;
        // }


        // This is using Unit Of Work
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _unitOfWork;
        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, 
            IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
            _mapper = mapper;
        }

        // [HttpGet]
        // [AllowAnonymous]
        // public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        // {
        //     //return await _context.Users.ToListAsync();
        //     return Ok(await _userRepository.GetUsersAsync());
        // }

        // [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
        {
            //return await _context.Users.ToListAsync();
            //return Ok(await _userRepository.GetUsersAsync());
            // var users = await _userRepository.GetUsersAsync();
            // var usersToReturn = _mapper.Map<IEnumerable<MemberDTO>>(users);
            // return Ok(usersToReturn);

            // var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            // 

            // This is using Unit Of Work.
            // var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            // Optimizing the code.
            var gender = await _unitOfWork.UserRepository.GetUserGender(User.GetUsername());

            // userParams.CurrentUsername = user.UserName;

            userParams.CurrentUsername = User.GetUsername();

            // if (string.IsNullOrEmpty(userParams.Gender))
            //     userParams.Gender = user.Gender == "male" ? "female" : "male";

            // Optimizing the code. 
            if (string.IsNullOrEmpty(userParams.Gender))
                userParams.Gender = gender == "male" ? "female" : "male";
                
            // var users = await _userRepository.GetMembersAsync(userParams);

            // This is using Unit Of Work.
            var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, 
                users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        // [Authorize]
        // api/users/id
        // [HttpGet("{id}")]
        // public async Task<ActionResult<AppUser>> GetUser(int id)
        // {
        //     return await _context.Users.FindAsync(id);
        // }

        // [HttpGet("{username}")]
        // public async Task<ActionResult<AppUser>> GetUser(string username)
        // {
        //     return await _userRepository.GetUserByUsernameAsync(username);
        // }

        //[Authorize(Roles = "Member")]
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            // var user = await _userRepository.GetUserByUsernameAsync(username);
            // return _mapper.Map<MemberDTO>(user);
            // return await _userRepository.GetMemberAsync(username);
            return await _unitOfWork.UserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
        {
            //var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var username = User.GetUsername();

            // var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // This is using Unit Of Work.
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            _mapper.Map(memberUpdateDTO, user);

            // _userRepository.Update(user);

            // This is using Unit Of Work.
            _unitOfWork.UserRepository.Update(user);

            // if (await _userRepository.SaveAllAsync()) return NoContent();
            
            // This is using Unit Of Work.
             if (await _unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to update user.");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            // var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // This is using Unit Of Work.
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);
            
            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            // if (user.Photos.Count == 0)
            // {
            //     photo.IsMain = true;
            // }

            user.Photos.Add(photo);

            // if (await _userRepository.SaveAllAsync())
            // {
            //     //return _mapper.Map<PhotoDTO>(photo);
            //     //return CreatedAtRoute("GetUser", _mapper.Map<PhotoDTO>(photo));
            //     return CreatedAtRoute("GetUser", new {username = user.UserName}, _mapper.Map<PhotoDTO>(photo));
            // }

            // This is using Unit Of Work.
            if (await _unitOfWork.Complete())
            {
                //return _mapper.Map<PhotoDTO>(photo);
                //return CreatedAtRoute("GetUser", _mapper.Map<PhotoDTO>(photo));
                return CreatedAtRoute("GetUser", new {username = user.UserName}, _mapper.Map<PhotoDTO>(photo));
            }
                

            return BadRequest("Problem adding photo.");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            // var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // This is using Unit Of Work.
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if(photo.IsMain) return BadRequest("This is already your main photo.");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;

            // if (await _userRepository.SaveAllAsync()) return NoContent();

            // This is using Unit Of Work
            if (await _unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to set main photo.");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            // var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // This is using Unit Of Work.
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo == null) return NotFound();

            if (photo.IsMain) return BadRequest("You cannot delete your main photo.");

            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);

                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            // if (await _userRepository.SaveAllAsync()) return Ok();

            // This is using Unit Of Work
            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Failed to delete photo.");
        }

    }
}