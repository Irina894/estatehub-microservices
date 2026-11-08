// ============================================================
// EstateHub_Activity — Проєкт №3 (MongoDB)
// Контекст: Комунікація та активність (Activity & Communication)
//
// Запуск: mongosh < db/p3/collections.js
// Ідемпотентно: якщо колекція вже існує з валідатором — пропускається.
// ============================================================

use EstateHub_Activity;

function ensureCollection(name, options) {
  const exists = db.getCollectionNames().includes(name);
  if (exists) {
    print(`Колекція ${name} вже існує — пропускаю createCollection.`);
    return;
  }
  db.createCollection(name, options);
  print(`Створено колекцію ${name} з валідатором.`);
}

// ---------- 1. Favorites ----------
ensureCollection("Favorites", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["_id", "clientId", "propertyId", "propertyTitle", "city", "addedAt"],
      properties: {
        _id:           { bsonType: "binData" },
        clientId:      { bsonType: "binData" },
        propertyId:    { bsonType: "binData" },
        propertyTitle: { bsonType: "string", maxLength: 200 },  // дубль з Каталогу
        city:          { bsonType: "string", maxLength: 100 },  // дубль з Каталогу
        addedAt:       { bsonType: "date" },
        createdBy:     { bsonType: ["binData", "null"] },
        updatedAt:     { bsonType: ["date", "null"] },
        updatedBy:     { bsonType: ["binData", "null"] }
      }
    }
  },
  validationLevel: "strict",
  validationAction: "error"
});

// ---------- 2. Reviews ----------
ensureCollection("Reviews", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["_id", "propertyId", "authorId", "rating", "createdAt"],
      properties: {
        _id:        { bsonType: "binData" },
        propertyId: { bsonType: "binData" },
        authorId:   { bsonType: "binData" },
        realtorId:  { bsonType: ["binData", "null"] },        // reference (необов'язковий)
        rating:     { bsonType: "int", minimum: 1, maximum: 5 },
        text:       { bsonType: "string" },
        criteria: {                                            // embed: оцінки за критеріями
          bsonType: "object",
          properties: {
            location:  { bsonType: "int", minimum: 1, maximum: 5 },
            condition: { bsonType: "int", minimum: 1, maximum: 5 },
            price:     { bsonType: "int", minimum: 1, maximum: 5 }
          }
        },
        comments: {                                            // embed: масив відповідей
          bsonType: "array",
          items: {
            bsonType: "object",
            required: ["authorId", "text", "createdAt"],
            properties: {
              authorId:  { bsonType: "binData" },
              text:      { bsonType: "string" },
              createdAt: { bsonType: "date" }
            }
          }
        },
        createdAt: { bsonType: "date" },
        createdBy: { bsonType: ["binData", "null"] },
        updatedAt: { bsonType: ["date", "null"] },
        updatedBy: { bsonType: ["binData", "null"] }
      }
    }
  },
  validationLevel: "strict",
  validationAction: "error"
});

// ---------- 3. Inquiries ----------
ensureCollection("Inquiries", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["_id", "propertyId", "clientId", "recipientId", "recipientType",
                 "type", "status", "messages", "createdAt"],
      properties: {
        _id:           { bsonType: "binData" },
        propertyId:    { bsonType: "binData" },                // reference
        propertyTitle: { bsonType: "string", maxLength: 200 }, // дубль
        city:          { bsonType: "string", maxLength: 100 }, // дубль
        clientId:      { bsonType: "binData" },
        recipientId:   { bsonType: "binData" },
        recipientType: { enum: ["owner", "realtor"] },
        type:          { enum: ["question", "viewing_request", "offer"] },
        status:        { enum: ["open", "closed"] },
        preferredDate: { bsonType: "date" },                   // тільки для viewing_request
        offerAmount:   { bsonType: "decimal" },                // тільки для offer
        messages: {                                            // embed: уся переписка
          bsonType: "array",
          minItems: 1,
          items: {
            bsonType: "object",
            required: ["senderId", "senderRole", "text", "sentAt"],
            properties: {
              senderId:   { bsonType: "binData" },
              senderRole: { enum: ["client", "owner", "realtor"] },
              text:       { bsonType: "string" },
              sentAt:     { bsonType: "date" }
            }
          }
        },
        createdAt: { bsonType: "date" },
        createdBy: { bsonType: ["binData", "null"] },
        updatedAt: { bsonType: ["date", "null"] },
        updatedBy: { bsonType: ["binData", "null"] }
      },
      // Гнучка схема: різні обов'язкові поля залежно від type
      anyOf: [
        { properties: { type: { enum: ["viewing_request"] } }, required: ["preferredDate"] },
        { properties: { type: { enum: ["offer"] } },           required: ["offerAmount"] },
        { properties: { type: { enum: ["question"] } } }
      ]
    }
  },
  validationLevel: "strict",
  validationAction: "error"
});

// ---------- Індекси під конкретні запити ----------
db.Inquiries.createIndex({ propertyId: 1 });
db.Inquiries.createIndex({ status: 1 });   // «швидка вибірка відкритих звернень»
db.Favorites.createIndex({ clientId: 1 });
db.Reviews.createIndex({ propertyId: 1 });

print("Готово: колекції Favorites, Reviews, Inquiries перевірені/створені з валідаторами.");
