# Unimall Product API and Client Application

## Project Overview
Unimall is a product crawling and transformation application that retrieves product information from Trendyol and provides enhanced product details.

## Prerequisites
- .NET 8.0 SDK
- Node.js 18+ 
- npm 9+

## Backend Setup (UnimallCase.Api)
1. Navigate to the API project directory
```bash
cd UnimallCase.Api
```

2. Restore dependencies
```bash
dotnet restore
```

3. Run the backend server
```bash
dotnet run
```

## Frontend Setup (UnimallCase.Client)
1. Navigate to the client project directory
```bash
cd UnimallCase.Client
```

2. Install dependencies
```bash
npm install
```

3. Serve the application
```bash
npm run dev
```

## Main Application Path
The main application entry point is:
- Backend: `UnimallCase.Api/Program.cs`
- Frontend: `UnimallCase.Client/src/main.js`

## Build for Production
### Backend
```bash
dotnet publish -c Release
```

### Frontend
```bash
npm run build
```

## Postman API Documentation

### Base URL
```
http://localhost:5000
```

### Endpoints

### 1. Crawl Product
Get raw product data from Trendyol URL.

```http
GET /api/Product/crawl
```

**Parameters:**
- `url` (query parameter): Trendyol product URL

**Example Request:**
```http
GET /api/Product/crawl?url=https://www.trendyol.com/english-home/bold-stripe-pamuklu-tek-kisilik-battaniye-150x200-cm-hardal-mavi-p-183244929
```

**Example Response:**
```json
{
    "name": "English Home Bold Stripe Pamuklu Tek Kişilik Battaniye 150x200 Cm Hardal - Mavi",
    "description": "Product description in Turkish",
    "sku": "183244929",
    "parentSku": "",
    "category": "Ev Tekstili > Battaniye > Tek Kişilik Battaniye",
    "brand": "English Home",
    "originalPrice": 819.0,
    "discountedPrice": 819.0,
    "url": "https://www.trendyol.com/english-home/...",
    "images": [
        "https://cdn.dsmcdn.com/image1.jpg"
    ],
    "attributes": [
        {
            "key": "Materyal",
            "name": "Pamuklu"
        }
    ]
}
```

### 2. Transform Product
Translate product data to English and calculate quality score.

```http
POST /api/Product/transform
```

**Headers:**
- Content-Type: application/json

**Request Body:**
```json
{
    "name": "Turkish product name",
    "description": "Turkish description",
    "sku": "123456",
    "parentSku": "",
    "category": "Turkish category",
    "brand": "Brand name",
    "originalPrice": 100.00,
    "discountedPrice": 80.00,
    "url": "https://www.trendyol.com/...",
    "images": [
        "https://cdn.dsmcdn.com/image1.jpg"
    ],
    "attributes": [
        {
            "key": "Turkish key",
            "name": "Turkish value"
        }
    ]
}
```

**Example Response:**
```json
{
    "name": "English product name",
    "description": "English description",
    "sku": "123456",
    "parentSku": "",
    "category": "English category",
    "brand": "Brand name",
    "originalPrice": 100.00,
    "discountedPrice": 80.00,
    "url": "https://www.trendyol.com/...",
    "images": [
        "https://cdn.dsmcdn.com/image1.jpg"
    ],
    "score": 85,
    "attributes": [
        {
            "key": "English key",
            "name": "English value"
        }
    ]
}
```

### 3. Crawl and Transform
Get product data from URL, translate to English, and calculate score in one call.

```http
GET /api/Product/crawl-and-transform
```

**Parameters:**
- `url` (query parameter): Trendyol product URL

**Example Request:**
```http
GET /api/Product/crawl-and-transform?url=https://www.trendyol.com/english-home/bold-stripe-pamuklu-tek-kisilik-battaniye-150x200-cm-hardal-mavi-p-183244929
```

**Example Response:**
```json
{
    "name": "English Home Bold Stripe Cotton Single Blanket 150x200 Cm Mustard - Blue",
    "description": "English description",
    "sku": "183244929",
    "parentSku": "",
    "category": "Home Textiles > Blankets > Single Blanket",
    "brand": "English Home",
    "originalPrice": 819.0,
    "discountedPrice": 819.0,
    "url": "https://www.trendyol.com/english-home/...",
    "images": [
        "https://cdn.dsmcdn.com/image1.jpg"
    ],
    "score": 85,
    "attributes": [
        {
            "key": "Material",
            "name": "Cotton"
        }
    ]
}
```

## Product Score Explanation

The score (0-100) is calculated based on:
- Name quality (25%): Length and content
- Description quality (20%): Length and detail
- Image count (35%):
  - 0 images: 0 points
  - 1 image: 10 points
  - 2 images: 20 points
  - 3 images: 25 points
  - 4 images: 30 points
  - 5+ images: 35 points
- Attributes (20%): 4 points per attribute

Penalties:
- Missing brand: -10%
- Missing category: -10%
- Less than 3 images: -20%
- Less than 3 attributes: -10%

## Error Responses

**400 Bad Request**
```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Bad Request",
    "status": 400,
    "detail": "Invalid Trendyol product URL"
}
```

**404 Not Found**
```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    "title": "Not Found",
    "status": 404,
    "detail": "Product not found at the specified URL"
}
```

**500 Internal Server Error**
```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    "title": "Internal Server Error",
    "status": 500,
    "detail": "An unexpected error occurred"
}

## Performance Optimizations
- Async processing with `ConfigureAwait(false)`
- Efficient LINQ transformations
- Minimal error handling overhead

## Troubleshooting
- Ensure all prerequisites are installed
- Check network connectivity
- Verify Trendyol URL format

## Contributing
1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License
[Specify your license here]
