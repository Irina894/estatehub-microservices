// ============================================================
// EstateHub_Activity — Проєкт №3: тестові дані (ідемпотентно)
// Запуск: mongosh < db/p3/seed.js
// Ідемпотентність: updateOne(filter, {$setOnInsert}, {upsert:true}) —
// find-or-create за природним ключем (пара client/property),
// повторний запуск не створює дублікатів.
// ============================================================

use EstateHub_Activity;

const seedPropertyId = UUID("11111111-1111-1111-1111-111111111111");
const seedClientId    = UUID("22222222-2222-2222-2222-222222222222");
const seedOwnerId     = UUID("33333333-3333-3333-3333-333333333333");
const seedRealtorId   = UUID("44444444-4444-4444-4444-444444444444");

// ---------- Favorites ----------
db.Favorites.updateOne(
  { clientId: seedClientId, propertyId: seedPropertyId },
  {
    $setOnInsert: {
      _id: UUID(),
      clientId: seedClientId,
      propertyId: seedPropertyId,
      propertyTitle: "SEED: Квартира в центрі Києва",
      city: "Київ",
      addedAt: new Date()
    }
  },
  { upsert: true }
);

// ---------- Reviews ----------
db.Reviews.updateOne(
  { propertyId: seedPropertyId, authorId: seedClientId },
  {
    $setOnInsert: {
      _id: UUID(),
      propertyId: seedPropertyId,
      authorId: seedClientId,
      rating: 5,
      text: "SEED: Чудова квартира, рекомендую.",
      criteria: { location: 5, condition: 4, price: 5 },
      comments: [],
      createdAt: new Date()
    }
  },
  { upsert: true }
);

// ---------- Inquiries: type = question ----------
db.Inquiries.updateOne(
  { propertyId: seedPropertyId, clientId: seedClientId, type: "question" },
  {
    $setOnInsert: {
      _id: UUID(),
      propertyId: seedPropertyId,
      propertyTitle: "SEED: Квартира в центрі Києва",
      city: "Київ",
      clientId: seedClientId,
      recipientId: seedOwnerId,
      recipientType: "owner",
      type: "question",
      status: "open",
      messages: [
        { senderId: seedClientId, senderRole: "client", text: "Чи можна з тваринами?", sentAt: new Date() }
      ],
      createdAt: new Date()
    }
  },
  { upsert: true }
);

// ---------- Inquiries: type = viewing_request ----------
db.Inquiries.updateOne(
  { propertyId: seedPropertyId, clientId: seedClientId, type: "viewing_request" },
  {
    $setOnInsert: {
      _id: UUID(),
      propertyId: seedPropertyId,
      propertyTitle: "SEED: Квартира в центрі Києва",
      city: "Київ",
      clientId: seedClientId,
      recipientId: seedRealtorId,
      recipientType: "realtor",
      type: "viewing_request",
      status: "open",
      preferredDate: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000),
      messages: [
        { senderId: seedClientId, senderRole: "client", text: "Хочу подивитись у суботу.", sentAt: new Date() }
      ],
      createdAt: new Date()
    }
  },
  { upsert: true }
);

// ---------- Inquiries: type = offer ----------
db.Inquiries.updateOne(
  { propertyId: seedPropertyId, clientId: seedClientId, type: "offer" },
  {
    $setOnInsert: {
      _id: UUID(),
      propertyId: seedPropertyId,
      propertyTitle: "SEED: Квартира в центрі Києва",
      city: "Київ",
      clientId: seedClientId,
      recipientId: seedOwnerId,
      recipientType: "owner",
      type: "offer",
      status: "open",
      offerAmount: NumberDecimal("2400000"),
      messages: [
        { senderId: seedClientId, senderRole: "client", text: "Пропоную 2 400 000.", sentAt: new Date() }
      ],
      createdAt: new Date()
    }
  },
  { upsert: true }
);

print("Готово: seed-дані Favorites/Reviews/Inquiries (question/viewing_request/offer) перевірені/вставлені.");
