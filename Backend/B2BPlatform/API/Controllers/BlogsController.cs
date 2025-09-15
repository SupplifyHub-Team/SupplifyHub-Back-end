using API.DTO;
using API.DTO.Blogs;
using API.DTO.GeneralResponse;
using API.Factory;
using Controllers;
using Entities;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using System.Linq.Expressions;
using YourNamespace;

namespace API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class BlogsController : APIBaseController
    {
        private readonly ICloudinaryService _cloudinaryService;

        public BlogsController(IUnitOfWork unitOfWork, IConfiguration configuration, ICloudinaryService cloudinary) : base(unitOfWork, configuration)
        {
            _cloudinaryService = cloudinary;
        }


        [HttpPost("post")]
        public async Task<IActionResult> CreateBlogAsync([FromForm] AddBlogDto addBlog)
        {
            ValidatePhoto(addBlog.CoverImageFile);
            if (addBlog.PdfFile != null)
                ValidatePdf(addBlog.PdfFile);
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));
            var coverImageUploadResult = await _cloudinaryService.UploadImageAsync(addBlog.CoverImageFile);
            if (string.IsNullOrEmpty(coverImageUploadResult.Url) || string.IsNullOrEmpty(coverImageUploadResult.PublicId))
                return BadRequest(ResponseFactory.CreateMessageResponse("حدث خطأ أثناء رفع صورة الغلاف. حاول مرة أخرى."));
            CloudinaryUploadResultDto pdfUploadResult = null;
            if (addBlog.PdfFile != null)
            {
                pdfUploadResult = await _cloudinaryService.UploadPdfForBlogAsync(addBlog.PdfFile);
                if (string.IsNullOrEmpty(pdfUploadResult.Url) || string.IsNullOrEmpty(pdfUploadResult.PublicId))
                    return BadRequest(ResponseFactory.CreateMessageResponse("حدث خطأ أثناء رفع ملف الـ PDF. حاول مرة أخرى."));
            }
            var blog = new Blog
            {
                Title = addBlog.Title.Trim(),
                Content = addBlog.Content.Trim(),
                Excerpt = addBlog.Excerpt.Trim(),
                CoverImageUrl = coverImageUploadResult.Url,
                PublicImageId = coverImageUploadResult.PublicId,
                PdfUrl = addBlog.PdfFile != null ? pdfUploadResult?.Url : null,
                PublicPdfId = addBlog.PdfFile != null ? pdfUploadResult?.PublicId : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Blogs.AddAsync(blog);
            if (await _unitOfWork.SaveAsync() > 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم إنشاء المدونة بنجاح."));
            return BadRequest(ResponseFactory.CreateMessageResponse("حدث خطأ أثناء إنشاء المدونة. حاول مرة أخرى."));
        }
        [HttpPatch("post/{id}")]
        public async Task<IActionResult> PatchBlog([FromRoute] int id, [FromForm] UpdateBlogDto dto)
        {
            if (id <=0)
                return BadRequest(ResponseFactory.CreateMessageResponse("المدونة غير موجودة"));
            var blog = await _unitOfWork.Blogs.FindAsync(b=>b.Id == id);
            if (blog == null)
            return BadRequest(ResponseFactory.CreateMessageResponse("المدونة غير موجودة"));
            if (!ModelState.IsValid)
                return BadRequest(ResponseFactory.CreateValidationErrorResponse(GetResponseForValidation()));

            // تعديل النصوص
            if (!string.IsNullOrEmpty(dto.Title))
                blog.Title = dto.Title;

            if (!string.IsNullOrEmpty(dto.Content))
                blog.Content = dto.Content;

            if (!string.IsNullOrEmpty(dto.Excerpt))
                blog.Excerpt = dto.Excerpt;

            // تعديل الصورة
            if (dto.CoverImage != null)
            {
                
                var result = await _cloudinaryService.UploadImageAsync(dto.CoverImage, blog.PublicImageId);
                blog.CoverImageUrl = result.Url;
                blog.PublicImageId = result.PublicId;
            }

            // تعديل PDF
            if (dto.PdfFile != null)
            {
                if (!string.IsNullOrEmpty(blog.PublicPdfId))
                    await _cloudinaryService.DeletePdfAsync(blog.PublicPdfId);
                var result = await _cloudinaryService.UploadPdfForBlogAsync(dto.PdfFile);
                blog.PdfUrl = result.Url;
                blog.PublicPdfId = result.PublicId;
            }

            blog.UpdatedAt = DateTime.UtcNow;

            if (await _unitOfWork.SaveAsync() > 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم تحديث المدونة بنجاح."));

            return(BadRequest(ResponseFactory.CreateMessageResponse("حدث خطأ أثناء تحديث المدونة. حاول مرة أخرى.")));
        }

        [HttpDelete("post/{id}")]
        public async Task<IActionResult> DeleteBlog([FromRoute]int id)
        {
            if (id <= 0)
                return BadRequest(ResponseFactory.CreateMessageResponse("المدونة غير موجودة"));
            var blog = await _unitOfWork.Blogs.GetByIdAsync(id);
            if (blog == null)
                return NotFound(ResponseFactory.CreateMessageResponse("المقال غير موجود"));

            
            if (!string.IsNullOrEmpty(blog.PublicImageId))
                await _cloudinaryService.DeleteAsync(blog.PublicImageId);

            
            if (!string.IsNullOrEmpty(blog.PublicPdfId))
                await _cloudinaryService.DeletePdfAsync(blog.PublicPdfId);

            
            _unitOfWork.Blogs.Delete(blog);

            if (await _unitOfWork.SaveAsync() > 0)
                return Ok(ResponseFactory.CreateMessageResponse("تم حذف المقال بنجاح"));

            return BadRequest(ResponseFactory.CreateMessageResponse("فشل حذف المقال"));
        }

        [HttpGet("posts")]
        public async Task<IActionResult> GetAllPosts(int page = 1,int pageSize = 10)
        {
            Expression<Func<Blog ,bool>> filter =  b => true;
            var totalCount = await _unitOfWork.Blogs.CountAsync(filter);
            var posts = await _unitOfWork.Blogs.FindWithFiltersAsync(
                10,
                criteria: filter,
                skip: (page - 1) * pageSize,
                take: pageSize,
                sortColumn: "CreatedAt",
                sortColumnDirection: "Desc"
            );
            var result =  posts.Select(p=> new PostPaginationDto() 
            {
                Id = p.Id,
                Slug = p.Title.ToLower().Replace(' ','-'),
                Title = p.Title,
                Excerpt = p.Excerpt,
                CoverImageUrl = p.CoverImageUrl,
                CreatedAt = p.CreatedAt,
            }).ToList();
            return Ok(ResponseFactory.CreatePaginationResponse(result , new Meta
            {
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize), 
                TotalItems = totalCount
            }));
        }

        [HttpGet("post")]
        public async Task<IActionResult> GetAllPosts(string slug)
        {
            var post = await _unitOfWork.Blogs.FindAsync(p=> p.Title.ToLower() == slug.Replace('-', ' '));
            if (post is null)
                return BadRequest(ResponseFactory.CreateMessageResponse("هناك مشكله في الخدم يجب المحاولة في وقت لاحق و أذا أستمرة المشكله بالرجاء التوجة ألي فريق الدعم"));
            var result = new PostDetailDto()
            {
                Title = post.Title,
                Content = post.Content,
                PdfUrl = post.PdfUrl,
                CoverImageUrl = post.CoverImageUrl,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
            };
            return Ok(ResponseFactory.CreateGeneralSingleResponse(result));
        }

    }
}
