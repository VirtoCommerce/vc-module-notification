<template>
  <VcBlade
    :title="item.subject || displayName(item.notificationType)"
    :subtitle="displayName(item.notificationType)"
    :loading="loading"
    width="60%"
  >
    <VcContainer>
      <div class="ntf-form tw-space-y-4">
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.MESSAGE.FIELDS.ID')"
              :model-value="item.id"
              copyable
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.STATUS')"
              :model-value="item.status"
            />
          </VcCol>
        </VcRow>
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.CREATED')"
              :model-value="item.createdDate"
              type="date"
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.MESSAGE.FIELDS.SENT')"
              :model-value="item.sendDate"
              type="date"
            />
          </VcCol>
        </VcRow>
        <VcRow class="tw-gap-4">
          <VcCol v-if="email">
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.FROM')"
              :model-value="item.from"
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.RECIPIENT')"
              :model-value="email ? item.to : item.number"
            />
          </VcCol>
        </VcRow>
        <VcRow
          v-if="email && (item.cc?.length || item.bcc?.length || item.replyTo)"
          class="tw-gap-4"
        >
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.CC')"
              :model-value="item.cc?.join(', ')"
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.BCC')"
              :model-value="item.bcc?.join(', ')"
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.REPLY_TO')"
              :model-value="item.replyTo"
            />
          </VcCol>
        </VcRow>
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.ATTEMPTS')"
              :model-value="`${item.sendAttemptCount ?? 0}/${item.maxSendAttemptCount ?? 0}`"
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.MESSAGE.FIELDS.LANGUAGE')"
              :model-value="item.languageCode"
            />
          </VcCol>
        </VcRow>
        <VcField
          v-if="item.lastSendError"
          :label="$t('NOTIFICATIONS.PAGES.MESSAGE.FIELDS.ERROR')"
          :model-value="item.lastSendError"
        />
        <VcCard
          v-if="email"
          :header="$t('NOTIFICATIONS.PAGES.MESSAGE.BODY')"
        >
          <!-- sandboxed: the sent e-mail's styles and scripts stay out of the shell -->
          <iframe
            class="ntf-preview"
            sandbox=""
            :srcdoc="item.body"
            :title="$t('NOTIFICATIONS.PAGES.MESSAGE.BODY')"
          />
        </VcCard>
        <VcCard
          v-else
          :header="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.MESSAGE')"
        >
          <div class="ntf-message">{{ item.message }}</div>
        </VcCard>
      </div>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade } from "@vc-shell/framework";
import { VcBlade, VcCard, VcCol, VcContainer, VcField, VcRow } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { displayName, isEmail, useNotificationClients, type MessageEntity } from "../api";

defineBlade({
  url: "/notification-message",
  name: "NotificationMessageDetails",
  permissions: ["notifications:read"],
});

const { param } = useBlade();
const clients = useNotificationClients();

const item = ref<MessageEntity>({});
const email = computed(() => isEmail(item.value.kind));

const { loading, action: load } = useAsync(async () => {
  item.value = await (await clients.notifications()).getObjectNotificationJournal(param.value!);
});

onMounted(load);
</script>

<style lang="scss">
.ntf-message {
  padding: 16px;
  white-space: pre-wrap;
  font-size: 14px;
}
</style>
