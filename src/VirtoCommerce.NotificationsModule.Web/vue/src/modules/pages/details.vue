<template>
  <VcBlade
    :title="displayName(param)"
    :subtitle="$t(email ? 'NOTIFICATIONS.KIND.EMAIL' : 'NOTIFICATIONS.KIND.SMS')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcContainer>
      <VcForm class="ntf-form tw-space-y-4">
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcField
              :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.TYPE')"
              :model-value="item.type"
              copyable
            />
          </VcCol>
          <VcCol>
            <VcSwitch
              v-model="item.isActive"
              :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.ACTIVE')"
            />
          </VcCol>
        </VcRow>
        <template v-if="email">
          <VcRow class="tw-gap-4">
            <VcCol>
              <VcInput
                v-model="item.from"
                type="email"
                :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.FROM')"
                :hint="$t('NOTIFICATIONS.PAGES.DETAILS.BLANK_HINT')"
              />
            </VcCol>
            <VcCol>
              <VcInput
                v-model="item.to"
                type="email"
                :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.TO')"
                :hint="$t('NOTIFICATIONS.PAGES.DETAILS.BLANK_HINT')"
              />
            </VcCol>
          </VcRow>
          <VcRow class="tw-gap-4">
            <VcCol>
              <VcMultivalue
                :model-value="toChips(item.cc)"
                :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.CC')"
                :hint="$t('NOTIFICATIONS.PAGES.DETAILS.LIST_HINT')"
                @update:model-value="(v: Chip[]) => (item.cc = fromChips(v))"
              />
            </VcCol>
            <VcCol>
              <VcMultivalue
                :model-value="toChips(item.bcc)"
                :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.BCC')"
                :hint="$t('NOTIFICATIONS.PAGES.DETAILS.LIST_HINT')"
                @update:model-value="(v: Chip[]) => (item.bcc = fromChips(v))"
              />
            </VcCol>
          </VcRow>
          <VcInput
            v-model="item.replyTo"
            type="email"
            :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.REPLY_TO')"
            :hint="$t('NOTIFICATIONS.PAGES.DETAILS.BLANK_HINT')"
          />
        </template>
        <VcInput
          v-else
          v-model="item.number"
          type="tel"
          :label="$t('NOTIFICATIONS.PAGES.DETAILS.FIELDS.NUMBER')"
          :hint="$t('NOTIFICATIONS.PAGES.DETAILS.BLANK_HINT')"
        />
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { defineBladeContext, useAsync, useBlade, useBladeForm, useBladeWidgets, useLoading, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcCol, VcContainer, VcField, VcForm, VcInput, VcMultivalue, VcRow, VcSwitch } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { displayName, effectiveTemplates, isEmail, loadNotification, toSaveModel, useNotificationClients, type NotificationEntity } from "../api";

defineBlade({
  url: "/notification",
  name: "NotificationDetails",
  permissions: ["notifications:read"],
});

const { t } = useI18n({ useScope: "global" });
const { param, options, openBlade, callParent, exposeToChildren } = useBlade<{ tenantId?: string; tenantType?: string }>();
const clients = useNotificationClients();

const item = ref<NotificationEntity>({});
const email = computed(() => isEmail(item.value.kind));

const form = useBladeForm({ data: item, closeConfirmMessage: computed(() => t("NOTIFICATIONS.ALERTS.CLOSE_CONFIRMATION")) });

// cc / bcc: one chip per address, as the AngularJS tags-input (VcMultivalue labels text chips by `title`)
type Chip = { id?: string; title: string };
const toChips = (list?: string[]): Chip[] => (list ?? []).map((title) => ({ title }));
const fromChips = (chips: Chip[]) => chips.map((x) => x.title);

const { loading: loadLoading, action: load } = useAsync(async () => {
  item.value = await loadNotification(await clients.notifications(), param.value!, options.value?.tenantId, options.value?.tenantType);
  form.setBaseline();
});

const { loading: saveLoading, action: save } = useAsync(async () => {
  await (await clients.notifications()).updateNotification(param.value!, toSaveModel(item.value));
  await Promise.all([load(), callParent("reload").catch(() => undefined)]);
});

const loading = useLoading(loadLoading, saveLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: t("NOTIFICATIONS.TOOLBAR.SAVE"),
    icon: "lucide-save",
    permissions: ["notifications:update"],
    disabled: computed(() => !form.canSave.value),
    clickHandler: () => save(),
  },
  {
    id: "reset",
    title: t("NOTIFICATIONS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
]);

const tenant = () => ({ tenantId: options.value?.tenantId, tenantType: options.value?.tenantType });

useBladeWidgets([
  {
    id: "NotificationTemplates",
    icon: "lucide-file-code",
    title: "NOTIFICATIONS.WIDGETS.TEMPLATES",
    badge: computed(() => effectiveTemplates(item.value.templates).length),
    onClick: () => openBlade({ name: "NotificationTemplates", param: param.value, options: tenant() }),
  },
  {
    id: "NotificationLog",
    icon: "lucide-history",
    title: "NOTIFICATIONS.WIDGETS.LOG",
    onClick: () => openBlade({ name: "NotificationsJournal", options: { notificationType: param.value } }),
  },
]);

defineBladeContext({ item });

onMounted(load);

// a template saved in a child blade changes this notification: take the server's version
exposeToChildren({
  reload: () => Promise.all([load(), callParent("reload").catch(() => undefined)]),
});
</script>
