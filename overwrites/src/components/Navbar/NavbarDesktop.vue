<template>
    <nav
      class="hidden md:block lg:block xl:block items-center justify-between flex-wrap container mx-auto py-3 z-20 dark:text-gray-400"
    >
      <div class="flex-grow flex items-center w-auto mx-4">
        <div class="flex items-center flex-shrink-0 mr-6">
          <span class="font-semibold text-xl tracking-tight">{{ $static.metadata.siteName }}</span>
        </div>
        <div class="flex-grow">
          <ul class="list-none flex justify-left">
            <li v-for="navItem in $static.metadata.headerNavigation" :key="navItem.name" class="px-4 py-1">
              <g-link
                class="block py-1"
                :to="navItem.link"
                :title="navItem.name"
                v-if="navItem.external!=true && navItem.children.length <=0"
              >{{ navItem.name}}</g-link>
              <a
                class="block py-1"
                :href="navItem.link"
                target="_blank"
                :title="navItem.name"
                v-if="navItem.external==true && navItem.children.length <=0 && navItem.name != '깃헙'"
              >{{ navItem.name}}</a>
              <a
                class="block py-1"
                :href="navItem.link"
                target="_blank"
                :title="navItem.name"
                v-if="navItem.external==true && navItem.children.length <=0 && navItem.name == '깃헙'"
              ><font-awesome :icon="['fab', 'github']"></font-awesome> {{ navItem.name}}</a>
              <ClientOnly>
              <v-popover
                placement="top"
                popoverClass="navbar-popover"
                offset="16"
                v-if="navItem.children.length > 0">
                <a class="block py-1" style="cursor:pointer;">
                  {{ navItem.name }}
                  <font-awesome :icon="['fas', 'angle-down']"></font-awesome>
                </a>

                <template slot="popover">
                  <ul>
                    <li v-for="subItem in navItem.children" :key="subItem.name" class="px-4 py-2 submenu-item hover:text-white">
                      <g-link
                        class="block"
                        :to="subItem.link"
                        :title="subItem.name"
                        v-if="subItem.external!=true"
                      >{{ subItem.name}}</g-link>
                      <a
                        class="block"
                        :href="subItem.link"
                        target="_blank"
                        :title="subItem.name"
                        v-if="subItem.external==true"
                      >{{ subItem.name}}</a>
                    </li>
                  </ul>
                </template>
              </v-popover>
              </ClientOnly>
            </li>
            <li class="px-4 py-1">
              <a role="button"
                @click.prevent="toggleSubNavigation()"
                class="block px-4 py-1"
                aria-label="Open Subnavigation"
                title="Open Subnavigation"
                v-bind:class="{
                  'text-blue-600' : showSubNavigation,
                  '' : !showSubNavigation
                }">
               <font-awesome :icon="['fas', 'ellipsis-h']" size="lg"></font-awesome>
              </a>

              <div v-click-outside="onClickOutside"
                class="py-4 mega-menu mb-16 border-t border-gray-200 shadow-xl bg-white dark:bg-black dark:border-gray-900"
                v-bind:class="{
                  '' : showSubNavigation,
                  'hidden' : !showSubNavigation
              }">

                <div>
                <subnavigation/>
                </div>
              </div>

            </li>
          </ul>
        </div>

        <div class="inline-block">
          <ul class="list-none flex justify-center md:justify-end">
            <li class="mr-6">
              <search-button v-on="$listeners"></search-button>
            </li>
            <li>
              <theme-switcher v-on="$listeners" :theme="theme" />
            </li>
          </ul>
        </div>
      </div>
    </nav>
</template>

<script>
import ThemeSwitcher from "~/components/Navbar/ThemeSwitcher.vue";
import SearchButton from "~/components/Navbar/SearchButton.vue";
import Subnavigation from "~/components/Navbar/NavbarSubNavigation.vue";

export default {
  data: function() {
    return {
      showSubNavigation: false,
      vcoConfig: {
        events: ["dblclick", "click"],
        isActive: true
      }
    };
  },
  components: {
    ThemeSwitcher,
    SearchButton,
    Subnavigation
  },
  props: {
    theme: {
      type: String
    },
    hideSubnav: {
      type: Boolean
    }
  },
  methods: {
    toggleSubNavigation() {
      this.showSubNavigation = !this.showSubNavigation;
    },
    onClickOutside(event) {
      if (!event.defaultPrevented && this.showSubNavigation == true) {
        this.toggleSubNavigation();
      }
    },
    hideSubNavigation() {
      this.showSubNavigation = false;
    }
  },
  watch: {
    hideSubnav() {
      if( this.hideSubnav ) {
        this.hideSubNavigation()
      }
    },
    $route (to, from){
      this.hideSubNavigation();
    }
  },

};
</script>

<static-query>
query {
  metadata {
    siteName
    headerNavigation {
      name
      link
      external
      children {
        name
        link
        external
      }
    }
  }
}
</static-query>
