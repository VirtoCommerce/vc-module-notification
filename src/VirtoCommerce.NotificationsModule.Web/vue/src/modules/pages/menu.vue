<template>
  <VcBlade
    :title="$t('NOTIFICATIONS.PAGES.MENU.TITLE')"
    width="40%"
  >
    <VcDataTable
      :items="rows"
      :active-item-id="activeItemId"
      :column-switcher="false"
      @row-click="(e: { data: Row }) => open(e.data)"
    >
      <VcColumn
        id="title"
        :title="$t('NOTIFICATIONS.PAGES.MENU.COLUMNS.SECTION')"
        always-visible
      >
        <template #body="{ data }">
          <span class="ntf-menu__item">
            <VcIcon
              :icon="data.icon"
              size="s"
            />
            {{ data.title }}
          </span>
        </template>
      </VcColumn>
      <VcColumn
        id="description"
        :title="$t('NOTIFICATIONS.PAGES.MENU.COLUMNS.DESCRIPTION')"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
// The AngularJS notifications workspace is a menu of three lists; kept as a narrow list blade.
import { useBlade } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcIcon } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

defineBlade({
  url: "/notifications",
  name: "NotificationsMenu",
  isWorkspace: true,
  permissions: ["notifications:access"],
  menuItem: { title: "NOTIFICATIONS.MENU.TITLE", icon: "lucide-mail", priority: 30 },
});

type Row = { id: string; blade: string; icon: string; title: string; description: string };

const { t } = useI18n({ useScope: "global" });
const { openBlade, query } = useBlade();
const route = useRoute();
const activeItemId = ref<string>();

const rows = computed<Row[]>(() => [
  {
    id: "journal",
    blade: "NotificationsJournal",
    icon: "lucide-history",
    title: t("NOTIFICATIONS.PAGES.JOURNAL.TITLE"),
    description: t("NOTIFICATIONS.PAGES.MENU.JOURNAL"),
  },
  {
    id: "list",
    blade: "NotificationsList",
    icon: "lucide-list",
    title: t("NOTIFICATIONS.PAGES.LIST.TITLE"),
    description: t("NOTIFICATIONS.PAGES.MENU.LIST"),
  },
  {
    id: "layouts",
    blade: "NotificationLayouts",
    icon: "lucide-layout-template",
    title: t("NOTIFICATIONS.PAGES.LAYOUTS.TITLE"),
    description: t("NOTIFICATIONS.PAGES.MENU.LAYOUTS"),
  },
]);

// the lists open without a param: the row is highlighted once its blade opened
function open(row: Row) {
  openBlade({ name: row.blade, onOpen: () => (activeItemId.value = row.id), onClose: () => (activeItemId.value = undefined) });
}

// /notifications?type=<type>[&templateId=<id>]: the AngularJS deep link, straight into the template editor
// (without templateId: the default-language template). The host restores the address after this blade
// mounts, so watch for the parameters instead of reading them once.
let deepLinked = false;
const linkParam = (name: string) => {
  const value = query.value?.[name] ?? route.query[name];
  return typeof value === "string" && value ? value : undefined;
};
watch(
  () => [linkParam("type"), linkParam("templateId")] as const,
  ([type, templateId]) => {
    if (!type || deepLinked) return;
    deepLinked = true;
    openBlade({
      name: "NotificationTemplateDetails",
      param: type,
      query: templateId ? { templateId } : { language: "default" },
    });
  },
  { immediate: true },
);
</script>

<style lang="scss">
.ntf-menu__item {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: var(--neutrals-900);
}
</style>
