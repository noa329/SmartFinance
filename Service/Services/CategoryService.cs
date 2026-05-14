using AutoMapper;
using Common.Dto;
using Common.Enum;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;

namespace Service.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IGooglePlacesService _googlePlacesService;
        private readonly IRepository<Expense> _expenseRepository;


        public CategoryService(IRepository<Category> categoryRepository, IMapper mapper, 
            IGooglePlacesService googlePlacesService, IRepository<Expense> expenseRepository)

        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _googlePlacesService = googlePlacesService;
            _expenseRepository = expenseRepository;
        }

        // ======================
        // GET USER CATEGORIES
        // ======================
        public async Task<List<CategoryDto>> GetUserCategories(int userId)
        {
            var categories = await _categoryRepository
                .Find(c =>c.UserId == userId);

            return _mapper.Map<List<CategoryDto>>(categories);
        }

        // ======================
        // GET BY ID
        // ======================
        public async Task<CategoryDto?> GetById(int id, int userId)
        {
            var category = await _categoryRepository.GetById(id);

            if (category == null)
                return null;

            if (!category.IsSystem && category.UserId != userId)
                throw new UnauthorizedAccessException();

            return _mapper.Map<CategoryDto>(category);
        }

        // ======================
        // CREATE
        // ======================
        public async Task<CategoryDto> Create(CategoryCreateDto dto, int userId)
        {
            var category = _mapper.Map<Category>(dto);

            category.UserId = userId;
            category.IsSystem = false;

            await _categoryRepository.Add(category);

            return _mapper.Map<CategoryDto>(category);
        }

        // ======================
        // UPDATE
        // ======================
        public async Task<CategoryDto> Update(int id, CategoryUpdateDto dto, int userId)
        {
            var category = await _categoryRepository.GetById(id);

            if (category == null)
                throw new Exception("Category not found");

            //if (category.IsSystem)
            //    throw new Exception("Cannot update system category");

            if (category.UserId != userId)
                throw new UnauthorizedAccessException();

            _mapper.Map(dto, category);

            await _categoryRepository.Update(id, category);

            return _mapper.Map<CategoryDto>(category);
        }

        // ======================
        // DELETE
        // ======================
        public async Task Delete(int id, int userId)
        {
            var category = await _categoryRepository.GetById(id);

            if (category == null)
                throw new Exception("Category not found");

            if (category.IsSystem)
                throw new Exception("Cannot delete system category");

            if (category.UserId != userId)
                throw new UnauthorizedAccessException();

            await _categoryRepository.Delete(id);
        }

       
        public async Task<int> ResolveCategoryId(int userId, string merchantName)
        {
            Console.WriteLine($"\n[CategoryResolver] ========================================");
            Console.WriteLine($"[CategoryResolver] Resolving: '{merchantName}' for User: {userId}");
            Console.WriteLine($"[CategoryResolver] ========================================");

            var pastExpenses = await _expenseRepository.Find(e =>
                e.UserId == userId &&
                e.MerchantName.ToLower().Trim() == merchantName.ToLower().Trim() &&
                e.CategoryId != 0);

            var lastExpense = pastExpenses.OrderByDescending(e => e.TransactionDate).FirstOrDefault();

            if (lastExpense != null)
            {
                Console.WriteLine($"[CategoryResolver] ✅ HISTORY MATCH! Used CategoryId: {lastExpense.CategoryId} (from past expense)");
                return lastExpense.CategoryId;
            }

            Console.WriteLine($"[CategoryResolver] No history found for '{merchantName}'");

            var googleTypes = await _googlePlacesService.GetPlaceTypesAsync(merchantName);

            Console.WriteLine($"[CategoryResolver] Google returned {googleTypes.Count} types:");
            foreach (var type in googleTypes)
            {
                Console.WriteLine($"[CategoryResolver]   - {type}");
            }

            if (!googleTypes.Any())
            {
                Console.WriteLine("[CategoryResolver] ⚠️ Google returned no types");
                return await GetFallbackCategoryId(userId);
            }

            var userCategories = (await _categoryRepository.Find(c => c.UserId == userId)).ToList();

            Console.WriteLine($"[CategoryResolver] Found {userCategories.Count} categories for User {userId}:");
            foreach (var cat in userCategories)
            {
                Console.WriteLine($"[CategoryResolver]   - {cat.Name} (ID: {cat.Id}, IsSystem: {cat.IsSystem})");
            }

            if (!userCategories.Any())
            {
                Console.WriteLine($"[CategoryResolver] ⚠️ ERROR: No categories found for User {userId}!");
                Console.WriteLine($"[CategoryResolver] Did you call InitializeUserCategories?");
                return 1; 
            }

            var customCategories = userCategories.Where(c => !c.IsSystem).ToList();
            var systemCategories = userCategories.Where(c => c.IsSystem).ToList();

            Console.WriteLine($"[CategoryResolver] Custom categories: {customCategories.Count}");
            Console.WriteLine($"[CategoryResolver] System categories: {systemCategories.Count}");

            var matchedCategory = MatchByCategoryLogic(googleTypes, customCategories)
                                 ?? MatchByCategoryLogic(googleTypes, systemCategories);

            if (matchedCategory != null)
            {
                Console.WriteLine($"[CategoryResolver] ✅✅✅ SUCCESS! Matched to: '{matchedCategory.Name}' (ID: {matchedCategory.Id})");
                return matchedCategory.Id;
            }

            Console.WriteLine($"[CategoryResolver] ⚠️ No match found - using fallback");
            return await GetFallbackCategoryId(userId);
        }

        private async Task<int> GetFallbackCategoryId(int userId)
        {
            var userCategories = await _categoryRepository.Find(c => c.UserId == userId);

            var fallback = userCategories.FirstOrDefault(c => c.Name.Contains("אחר"));

            if (fallback != null)
            {
                Console.WriteLine($"[CategoryResolver] Fallback: 'אחר' (ID: {fallback.Id})");
                return fallback.Id;
            }

            var first = userCategories.FirstOrDefault();
            if (first != null)
            {
                Console.WriteLine($"[CategoryResolver] Fallback: '{first.Name}' (ID: {first.Id})");
                return first.Id;
            }

            Console.WriteLine($"[CategoryResolver] ⚠️⚠️⚠️ CRITICAL: No categories at all for User {userId}!");
            return 1;
        }

        private Category? MatchByCategoryLogic(List<string> googleTypes, List<Category> categories)
        {
            Console.WriteLine($"\n[CategoryResolver] Matching {googleTypes.Count} Google types against {categories.Count} categories...");

            foreach (var type in googleTypes)
            {
                var cleanGoogleType = type.Replace("_", " ").ToLower().Trim();

                if (IsGenericType(cleanGoogleType))
                {
                    Console.WriteLine($"[CategoryResolver]   ⏭️ Skipping generic type: '{cleanGoogleType}'");
                    continue;
                }

                Console.WriteLine($"[CategoryResolver]   🔍 Checking Google type: '{cleanGoogleType}'");

                foreach (var category in categories)
                {
                    if (IsMatch(cleanGoogleType, category))
                    {
                        Console.WriteLine($"[CategoryResolver]   ✅✅ MATCH FOUND!");
                        Console.WriteLine($"[CategoryResolver]      Google: '{cleanGoogleType}'");
                        Console.WriteLine($"[CategoryResolver]      Category: '{category.Name}' (ID: {category.Id})");
                        return category;
                    }
                }
            }

            Console.WriteLine($"[CategoryResolver]   ❌ No matches found");
            return null;
        }

        private bool IsGenericType(string type)
        {
            var genericTypes = new[]
            {
        "establishment",
        "point of interest",
        "service",
        "place",
        "store" 
    };

            return genericTypes.Contains(type);
        }

        private bool IsMatch(string googleType, Category category)
        {
            var synonyms = GetSynonyms(category.Name);

            if (!synonyms.Any())
                return false;

            foreach (var synonym in synonyms)
            {
                var cleanSynonym = synonym.ToLower().Trim();

                if (googleType == cleanSynonym)
                {
                    Console.WriteLine($"[CategoryResolver]      ✓ Exact: '{googleType}' == '{cleanSynonym}'");
                    return true;
                }

                if (googleType.Contains(cleanSynonym))
                {
                    Console.WriteLine($"[CategoryResolver]      ✓ Contains: '{googleType}' contains '{cleanSynonym}'");
                    return true;
                }

                if (cleanSynonym.Contains(googleType))
                {
                    Console.WriteLine($"[CategoryResolver]      ✓ Contained: '{cleanSynonym}' contains '{googleType}'");
                    return true;
                }
            }

            return false;
        }

        private List<string> GetSynonyms(string categoryName)
        {
            var name = categoryName.ToLower().Trim();

            return name switch
            {
                _ when name.Contains("קניות") || name.Contains("בגדים") || name.Contains("ביגוד")
                    => new List<string>
                {
            "clothing store", "clothing", "apparel", "fashion", "boutique", "shoe store",
            "electronics store", "electronics", "computer store", "shopping mall",
            "department store", "jewelry store", "gift shop", "home goods store",
            "book store", "pet store", "toy store"
                },

                _ when name.Contains("אוכל") || name.Contains("מזון") || name.Contains("מסעד") || name.Contains("קפה")
                    => new List<string>
                {
            "restaurant", "cafe", "coffee shop", "bakery", "bar", "fast food",
            "pizza place", "ice cream shop", "meal takeaway", "meal delivery",
            "supermarket", "grocery store", "grocery", "convenience store"
                },

                _ when name.Contains("תחבורה") || name.Contains("רכב") || name.Contains("נסיעות") || name.Contains("אוטו")
                    => new List<string>
                {
            "gas station", "fuel", "petrol station", "parking", "parking lot",
            "car repair", "car wash", "car dealer", "transit station",
            "bus station", "train station", "subway station", "taxi stand"
                },

                _ when name.Contains("בריאות") || name.Contains("פארם") || name.Contains("רפואה") || name.Contains("חולים")
                    => new List<string>
                {
            "pharmacy", "drugstore", "health", "hospital", "doctor", "dentist",
            "physiotherapist", "gym", "fitness center", "spa", "hair care"
                },

                _ when name.Contains("בילוי") || name.Contains("פנאי") || name.Contains("בידור") || name.Contains("חופש")
                    => new List<string>
                {
            "movie theater", "cinema", "amusement park", "night club", "bar",
            "museum", "art gallery", "aquarium", "zoo", "stadium", "tourist attraction"
                },

                _ when name.Contains("חשבונות") || name.Contains("שירות") || name.Contains("תשלום") || name.Contains("בנק")
                    => new List<string>
                {
            "bank", "atm", "finance", "post office", "accounting", "lawyer",
            "insurance agency", "electrician", "plumber", "local government office"
                },

                _ when name.Contains("דירה") || name.Contains("שכר דירה") || name.Contains("דיור")
                    => new List<string>
                {
            "real estate agency", "property management", "housing complex", "apartment building"
                },

                _ => new List<string>()
            };
        }


    }
}